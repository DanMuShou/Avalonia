using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Services;
using Serilog;
using Serilog.Extensions.Logging;
using SuperSocket;
using SuperSocket.Client;
using SuperSocket.ProtoBase;

namespace MiniToolBoxCross.Models.Services;

public class ClientForwardService(ILogger<ClientForwardService> logger) : IClientForwardService
{
    private IEasyClient<StringPackageInfo> _client = null!;
    private readonly ConcurrentDictionary<Guid, EchoServer> _forwardServers = new();
    private readonly ConcurrentDictionary<string, HashSet<EchoServer>> _forwardTargets = [];
    private readonly Lock _targetsLock = new();

    public IPEndPoint EndPoint { get; private set; } = null!;
    public bool IsConfigured { get; private set; }
    public bool IsConnected { get; private set; }

    public async Task<bool> ConfigAsync(IPEndPoint endPoint)
    {
        if (IsConfigured)
        {
            logger.LogWarning("客机已经配置过");
            return true;
        }

        var isValidPort = SocketHelper.CheckClientPortValidation(endPoint.Port);
        if (!isValidPort.HasValue)
        {
            logger.LogError("客机端口无效: {Port}", endPoint.Port);
            return false;
        }

        try
        {
            var filter = new CommandLinePipelineFilter
            {
                Decoder = new DefaultStringPackageDecoder(Encoding.UTF8),
            };
            var options = new SuperSocket.Connection.ConnectionOptions()
            {
                Logger = new SerilogLoggerProvider(Log.Logger).CreateLogger("EasyClient"),
            };
            _client = new EasyClient<StringPackageInfo>(filter, options).AsClient();

            _client.PackageHandler += async (_, package) =>
            {
                SendToForwardServers(package.Key, package.Body);
            };

            _client.Closed += (sender, e) =>
            {
                logger.LogError("客机已断开连接");
            };

            EndPoint = endPoint;
            IsConfigured = true;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客机配置失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("客机未配置，无法启动");
            return false;
        }

        if (IsConnected)
        {
            logger.LogWarning("客机已经在运行中");
            return true;
        }

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Start();
            await _client.ConnectAsync(EndPoint);
            _client.StartReceive();
            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客机启动失败: {Message}", ex.Message);
            IsConnected = false;
            return false;
        }
    }

    public async Task<bool> DisconnectAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("客机未配置，无法停止");
            return false;
        }

        if (!IsConnected)
        {
            logger.LogWarning("客机未在运行");
            return true;
        }

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Stop();
            await _client.CloseAsync();
            IsConnected = false;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客机停止失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ReconnectAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("客机未配置，无法重启");
            return false;
        }

        if (!IsConnected)
        {
            logger.LogWarning("客机未在运行，无法重启");
            return true;
        }

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Stop();
            await _client.CloseAsync();
            IsConnected = false;

            foreach (var server in _forwardServers.Values)
                server.Start();
            await _client.ConnectAsync(EndPoint);
            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客机重启失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<EchoServer?> AddForwardServerAsync(
        IPEndPoint endPoint,
        ForwardTargetType forwardTargetType
    )
    {
        if (IsConnected)
        {
            logger.LogError("客机正在运行，无法添加转发服务端");
            return null;
        }

        if (_forwardServers.Values.Any(server => server.Endpoint.Equals(endPoint)))
        {
            logger.LogWarning("转发服务端已存在: {EndPoint}", endPoint);
            return null;
        }

        if (!SocketHelper.GetIpAddressList().Contains(endPoint.Address))
        {
            logger.LogError("转发服务端地址未找到: {Address}", endPoint.Address);
            return null;
        }

        var validPort = SocketHelper.CheckServerPortValidation(endPoint.Port);
        if (!validPort.HasValue)
        {
            logger.LogError("没有找到转发服务端可用的网络端口: {Port}", endPoint.Port);
            return null;
        }
        var validIpEndPoint = new IPEndPoint(endPoint.Address, validPort.Value);

        try
        {
            var server = new EchoServer(validIpEndPoint, "Game", forwardTargetType);
            _forwardServers.TryAdd(server.Id, server);

            lock (_targetsLock)
            {
                if (!_forwardTargets.TryGetValue(server.ForwardTargetType, out var servers))
                {
                    servers = [];
                    _forwardTargets[server.ForwardTargetType] = servers;
                }
                servers.Add(server);
            }

            server.ErrorOccurred += async error =>
            {
                logger.LogError("转发服务端发生错误: {Error}", error);
            };

            server.DataReceived += async (buffer, offset, size) =>
            {
                await _client.SendAsync(
                    SocketHelper.StringPackInfoMessageEncoder(
                        server.Key,
                        server.ForwardTargetType,
                        buffer,
                        offset,
                        size
                    )
                );
            };

            return server;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加转发服务端失败: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> RemoveForwardServerAsync(Guid id)
    {
        if (IsConnected)
        {
            logger.LogError("客机正在运行，无法移除转发服务端");
            return false;
        }

        if (!_forwardServers.TryRemove(id, out var server))
        {
            logger.LogWarning("转发服务端不存在: {id}", id);
            return false;
        }

        try
        {
            if (_forwardTargets.TryGetValue(server.ForwardTargetType, out var servers))
            {
                lock (_targetsLock)
                {
                    servers.Remove(server);
                    if (servers.Count == 0)
                        _forwardTargets.TryRemove(server.ForwardTargetType, out _);
                }
            }
            server.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "移除转发服务端失败: {Id}, {Message}", id, ex.Message);
            return false;
        }
    }

    private void SendToForwardServers(string forwardTargetType, byte[] data, long offset, long size)
    {
        HashSet<EchoServer>? servers;
        lock (_targetsLock)
        {
            _forwardTargets.TryGetValue(forwardTargetType, out servers);
        }

        if (servers == null || servers.Count == 0)
            return;

        EchoServer[] snapshot;
        lock (_targetsLock)
        {
            snapshot = [.. servers];
        }

        foreach (var server in snapshot)
        {
            try
            {
                if (server.IsStarted && server.LastClientEndpoint is not null)
                    server.SendAsync(server.LastClientEndpoint, data, offset, size);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "转发服务端 {ServerId} 发送信息失败", server.Id);
            }
        }
    }

    private void SendToForwardServers(string forwardTargetType, string message)
    {
        HashSet<EchoServer>? servers;
        lock (_targetsLock)
        {
            _forwardTargets.TryGetValue(forwardTargetType, out servers);
        }

        if (servers == null || servers.Count == 0)
            return;

        EchoServer[] snapshot;
        lock (_targetsLock)
        {
            snapshot = [.. servers];
        }

        foreach (var server in snapshot)
        {
            try
            {
                if (server.IsStarted && server.LastClientEndpoint is not null)
                    server.SendAsync(server.LastClientEndpoint, message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "转发服务器发送信息 {ServerId} 失败", server.Id);
            }
        }
    }

    public async Task<bool> DisposeAsync()
    {
        if (!IsConfigured)
        {
            logger.LogWarning("客机未配置，无需释放");
            return true;
        }

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Dispose();
            _forwardServers.Clear();
            _forwardTargets.Clear();

            await _client.DisposeAsync();
            IsConfigured = false;
            IsConnected = false;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客机资源释放失败: {Message}", ex.Message);
            return false;
        }
    }
}

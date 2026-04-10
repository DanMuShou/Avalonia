using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Services;

public class ForwardClientService(ILogger<ForwardClientService> logger) : IForwardClientService
{
    public Action? OnErrorOccurred { get; set; }
    public bool IsConfigured { get; private set; }
    public bool IsConnected { get; private set; }
    private readonly ConcurrentDictionary<Guid, UdpEchoServer> _forwardServers = new();
    private readonly ConcurrentDictionary<byte, HashSet<UdpEchoServer>> _forwardTargets = [];
    private readonly Lock _targetsLock = new();
    private UdpEchoClient _client = null!;

    public bool Config(IPEndPoint endPoint)
    {
        if (IsConfigured)
        {
            logger.LogWarning("客机已配置");
            return true;
        }

        if (!SocketHelper.IsClientPortAvailable(endPoint.Port))
        {
            logger.LogError("客机连接端口不可用: {Port}", endPoint.Port);
            return false;
        }

        _client = new UdpEchoClient(endPoint)
        {
            OnErrorOccurred = ErrorOperation,
            OnReceivedData = SendData,
        };

        IsConfigured = true;
        return true;
    }

    public bool Connect()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法连接主机，客机未配置");
            return false;
        }

        if (IsConnected)
        {
            logger.LogWarning("无法连接主机客机已连接");
            return false;
        }

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "无法连接主机，无法启动转发服务器");
            return false;
        }

        _client.Connect();

        IsConnected = true;
        return true;
    }

    public bool Disconnect()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法断开主机，客机未配置");
            return false;
        }

        if (!IsConnected)
        {
            logger.LogWarning("无法断开主机，客机未连接");
            return false;
        }

        _client.Disconnect();
        foreach (var server in _forwardServers.Values)
            server.Stop();

        IsConnected = false;
        return true;
    }

    public bool Reconnect()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法重新连接主机，客机未配置");
            return false;
        }

        if (!IsConnected)
        {
            logger.LogWarning("无法重新连接主机，客机未连接");
            return false;
        }

        IsConnected = false;

        try
        {
            foreach (var server in _forwardServers.Values)
                server.Restart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "无法重新连接主机，转发服务器重启失败");
            return false;
        }

        _client.Reconnect();
        IsConnected = true;
        return true;
    }

    public UdpEchoServer? AddForwardServer(IPEndPoint endPoint, ForwardTargetType forwardTargetType)
    {
        if (IsConnected)
        {
            logger.LogError("无法添加转发服务器，客机已连接");
            return null;
        }

        if (_forwardServers.Values.Any(server => server.Endpoint.ToString() == endPoint.ToString()))
        {
            logger.LogError(
                "无法添加转发服务器，转发服务器 {Address}:{Port} 已存在",
                endPoint.Address,
                endPoint.Port
            );
            return null;
        }
        if (!SocketHelper.GetIpAddressList().Contains(endPoint.Address))
        {
            logger.LogError("无法添加转发服务器，服务器地址无效: {Address}", endPoint.Address);
            return null;
        }

        if (!SocketHelper.IsServerPortAvailable(endPoint.Port))
        {
            logger.LogError("无法添加转发服务器，服务器端口不可用: {Port}", endPoint.Port);
            return null;
        }

        var server = new UdpEchoServer(endPoint, forwardTargetType)
        {
            OnErrorOccurred = ErrorOperation,
        };
        _forwardServers.TryAdd(server.Id, server);
        lock (_targetsLock)
        {
            if (!_forwardTargets.TryGetValue(server.ForwardByte, out var servers))
            {
                servers = [];
                _forwardTargets[server.ForwardByte] = servers;
            }
            servers.Add(server);
        }

        server.OnReceivedData += (buffer, offset, size) =>
        {
            var totalSize = (int)size + 1;
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                rentedBuffer[0] = server.ForwardByte;
                Buffer.BlockCopy(buffer, (int)offset, rentedBuffer, 1, (int)size);
                _client.Send(rentedBuffer, 0, totalSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        };

        return server;
    }

    public bool RemoveForwardServer(Guid id)
    {
        if (IsConnected)
        {
            logger.LogError("无法移除转发服务器，客机已连接");
            return false;
        }

        if (!_forwardServers.TryRemove(id, out var server))
        {
            logger.LogError("无法移除转发服务器，转发服务器 {Id} 不存在", id);
            return false;
        }

        lock (_targetsLock)
        {
            if (_forwardTargets.TryGetValue(server.ForwardByte, out var servers))
            {
                servers.Remove(server);
                if (servers.Count == 0)
                    _forwardTargets.Remove(server.ForwardByte, out _);
            }
        }

        server.Dispose();
        return true;
    }

    public void SendData(byte[] buffer, long offset, long size)
    {
        var forwardTargetType = buffer[offset];
        HashSet<UdpEchoServer>? servers;
        lock (_targetsLock)
        {
            _forwardTargets.TryGetValue(forwardTargetType, out servers);
        }

        if (servers is null || servers.Count == 0)
            return;

        UdpEchoServer[] snapshot;
        lock (_targetsLock)
        {
            snapshot = [.. servers];
        }

        foreach (var server in snapshot)
        {
            server.Send(server.LastClientEndpoint, buffer, offset + 1, size - 1);
        }
    }

    private void ErrorOperation()
    {
        if (!IsConnected)
        {
            logger.LogError("无法执行错误处理，客机已处于错误状态");
            return;
        }

        _client.Dispose();
        foreach (var server in _forwardServers.Values)
            server.Dispose();

        _forwardServers.Clear();
        _forwardTargets.Clear();

        IsConnected = false;
        IsConfigured = false;
        OnErrorOccurred?.Invoke();
    }
}

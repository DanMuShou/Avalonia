using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Services;

public class ForwardHostService(ILogger<ForwardHostService> logger) : IForwardHostService
{
    public Action? OnErrorOccurred { get; set; }
    public bool IsConfigured { get; private set; }
    public bool IsStarted { get; private set; }

    private readonly ConcurrentDictionary<Guid, UdpEchoClient> _forwardClients = [];
    private readonly ConcurrentDictionary<byte, HashSet<UdpEchoClient>> _forwardTargets = [];
    private readonly Lock _targetsLock = new();
    private UdpEchoServer _server = null!;

    public bool Config(IPEndPoint endPoint)
    {
        if (IsConfigured)
        {
            logger.LogWarning("主机已配置");
            return true;
        }

        if (!SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6).Contains(endPoint.Address))
        {
            logger.LogError("主机监听地址未找到: {Address}", endPoint.Address);
            return false;
        }

        if (!SocketHelper.IsServerPortAvailable(endPoint.Port))
        {
            logger.LogError("主机监听端口 {Port} 不可用", endPoint.Port);
            return false;
        }

        _server = new UdpEchoServer(endPoint)
        {
            OnReceivedData = SendData,
            OnErrorOccurred = ErrorOperation,
        };

        IsConfigured = true;
        return true;
    }

    public bool Start()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法启动主机，未配置");
            return false;
        }

        if (IsStarted)
        {
            logger.LogError("无法启动主机，已经启动");
            return false;
        }

        try
        {
            _server.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "无法启动主机，监听服务启动失败");
            ErrorOperation();
            return false;
        }

        foreach (var client in _forwardClients.Values)
            client.Connect();

        IsStarted = true;
        return IsStarted;
    }

    public bool Stop()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法停止主机，未配置");
            return false;
        }

        if (!IsStarted)
        {
            logger.LogError("无法停止主机，已经停止");
            return false;
        }

        foreach (var client in _forwardClients.Values)
            client.Disconnect();
        _server.Stop();

        IsStarted = false;
        return !IsStarted;
    }

    public bool Restart()
    {
        if (!IsConfigured)
        {
            logger.LogError("无法重启主机，未配置");
            return false;
        }

        if (!IsStarted)
        {
            logger.LogError("无法重启主机，已经停止");
            return false;
        }

        IsStarted = false;

        try
        {
            _server.Restart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "无法重启主机，监听服务重启失败");
            ErrorOperation();
            return false;
        }

        foreach (var client in _forwardClients.Values)
            client.Reconnect();

        IsStarted = true;
        return true;
    }

    public UdpEchoClient? AddForwardClient(IPEndPoint endPoint, ForwardTargetType forwardTargetType)
    {
        if (IsStarted)
        {
            logger.LogError("无法添加转发客户端，主机已经启动");
            return null;
        }

        if (_forwardClients.Values.Any(client => client.Endpoint.ToString() == endPoint.ToString()))
        {
            logger.LogError(
                "无法添加转发客户端，转发客户端已存在: {Address}:{Port}",
                endPoint.Address,
                endPoint.Port
            );
            return null;
        }

        if (!SocketHelper.IsClientPortAvailable(endPoint.Port))
        {
            logger.LogError("无法添加转发客户端，转发客户端端口 {Port} 不可用", endPoint.Port);
            return null;
        }

        var client = new UdpEchoClient(endPoint, forwardTargetType)
        {
            OnErrorOccurred = ErrorOperation,
        };
        _forwardClients.TryAdd(client.Id, client);
        lock (_targetsLock)
        {
            if (!_forwardTargets.TryGetValue(client.ForwardByte, out var clients))
            {
                clients = [];
                _forwardTargets[client.ForwardByte] = clients;
            }
            clients.Add(client);
        }

        client.OnReceivedData = (buffer, offset, size) =>
        {
            var totalSize = (int)size + 1;
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                rentedBuffer[0] = client.ForwardByte;
                Buffer.BlockCopy(buffer, (int)offset, rentedBuffer, 1, (int)size);
                _server.Send(_server.LastClientEndpoint, rentedBuffer, 0, totalSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        };

        return client;
    }

    public bool RemoveForwardClient(Guid id)
    {
        if (IsStarted)
        {
            logger.LogError("无法移除转发客户端，主机已经启动");
            return false;
        }

        if (!_forwardClients.TryRemove(id, out var client))
        {
            logger.LogError("无法移除转发客户端，转发客户端不存在: {Id}", id);
            return false;
        }

        lock (_targetsLock)
        {
            if (_forwardTargets.TryGetValue(client.ForwardByte, out var clients))
            {
                clients.Remove(client);
                if (clients.Count == 0)
                    _forwardTargets.Remove(client.ForwardByte, out _);
            }
        }

        client.Dispose();
        return true;
    }

    private void SendData(byte[] buffer, long offset, long size)
    {
        var forwardTargetType = buffer[offset];
        HashSet<UdpEchoClient>? clients;
        lock (_targetsLock)
        {
            _forwardTargets.TryGetValue(forwardTargetType, out clients);
        }

        if (clients is null || clients.Count == 0)
            return;

        UdpEchoClient[] snapshot;
        lock (_targetsLock)
        {
            snapshot = [.. clients];
        }

        foreach (var client in snapshot)
        {
            client.Send(buffer, offset + 1, size - 1);
        }
    }

    private void ErrorOperation()
    {
        if (!IsStarted)
        {
            logger.LogError("无法进行错误处理，主机已经处于错误状态");
            return;
        }

        foreach (var client in _forwardClients.Values)
            client.Dispose();
        _server.Dispose();

        _forwardClients.Clear();
        _forwardTargets.Clear();

        IsStarted = false;
        IsConfigured = false;
        OnErrorOccurred?.Invoke();
    }
}

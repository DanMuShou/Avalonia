using System;
using System.Net;
using System.Net.Sockets;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services.NetCoreServer;

namespace MiniToolBoxCross.Services;

public class EchoServer(IPEndPoint endPoint, string key, ForwardTargetType forwardTargetType)
    : UdpServer(endPoint)
{
    public event Action? Started;
    public event Action<byte[], long, long>? DataReceived;
    public event Action<SocketError>? ErrorOccurred;
    public string ForwardTargetType { get; } = ((int)forwardTargetType).ToString();
    public string Key { get; } = key;

    public EndPoint? LastClientEndpoint { get; private set; }

    protected override void OnStarted()
    {
        base.OnStarted();
        Started?.Invoke();
        ReceiveAsync();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        base.OnReceived(endpoint, buffer, offset, size);
        LastClientEndpoint = endpoint;
        DataReceived?.Invoke(buffer, offset, size);
        ReceiveAsync();
    }

    protected override void OnError(SocketError error)
    {
        base.OnError(error);
        ErrorOccurred?.Invoke(error);
    }
}

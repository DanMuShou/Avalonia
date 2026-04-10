using System;
using System.Net;
using System.Net.Sockets;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services.NetCoreServer;

namespace MiniToolBoxCross.Services;

public class UdpEchoServer : UdpServer
{
    public Action? OnErrorOccurred;
    public Action<byte[], long, long>? OnReceivedData;
    public EndPoint LastClientEndpoint = null!;
    public byte ForwardByte { get; }

    public UdpEchoServer(IPEndPoint endPoint)
        : base(endPoint)
    {
        OptionReceiveBufferSize = 1024 * 1024;
        OptionSendBufferSize = 1024 * 1024;
        OptionReceiveBufferLimit = 2 * 1024 * 1024;
        OptionSendBufferLimit = 2 * 1024 * 1024;
    }

    public UdpEchoServer(IPEndPoint endPoint, ForwardTargetType forwardTargetType)
        : this(endPoint)
    {
        ForwardByte = (byte)forwardTargetType;
    }

    protected override void OnStarted()
    {
        base.OnStarted();
        ReceiveAsync();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        base.OnReceived(endpoint, buffer, offset, size);
        ReceiveAsync();
        LastClientEndpoint = endpoint;
        OnReceivedData?.Invoke(buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        base.OnError(error);
        OnErrorOccurred?.Invoke();
    }
}

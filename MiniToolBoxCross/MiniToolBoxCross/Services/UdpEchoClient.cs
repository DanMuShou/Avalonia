using System;
using System.Net;
using System.Net.Sockets;
using MiniToolBoxCross.Common.Enums;
using UdpClient = MiniToolBoxCross.Services.NetCoreServer.UdpClient;

namespace MiniToolBoxCross.Services;

public class UdpEchoClient : UdpClient
{
    public Action<byte[], long, long>? OnReceivedData;
    public Action? OnErrorOccurred;

    public byte ForwardByte { get; }

    public UdpEchoClient(IPEndPoint endPoint)
        : base(endPoint)
    {
        OptionReceiveBufferSize = 1024 * 1024;
        OptionSendBufferSize = 1024 * 1024;
        OptionReceiveBufferLimit = 2 * 1024 * 1024;
        OptionSendBufferLimit = 2 * 1024 * 1024;
    }

    public UdpEchoClient(IPEndPoint endPoint, ForwardTargetType forwardTargetType)
        : this(endPoint)
    {
        ForwardByte = (byte)forwardTargetType;
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        ReceiveAsync();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        base.OnReceived(endpoint, buffer, offset, size);
        ReceiveAsync();
        OnReceivedData?.Invoke(buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        base.OnError(error);
        OnErrorOccurred?.Invoke();
    }
}

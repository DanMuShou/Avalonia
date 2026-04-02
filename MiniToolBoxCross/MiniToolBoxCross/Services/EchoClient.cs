using System;
using System.Net;
using System.Net.Sockets;
using UdpClient = MiniToolBoxCross.Services.NetCoreServer.UdpClient;

namespace MiniToolBoxCross.Services;

public class EchoClient(IPEndPoint endPoint) : UdpClient(endPoint)
{
    /// <summary>
    /// 客户端连接成功事件
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// 客户端断开连接事件
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// 接收到数据事件
    /// </summary>
    public event EventHandler<(
        EndPoint endpoint,
        byte[] buffer,
        long offset,
        long size
    )>? DataReceived;

    /// <summary>
    /// 发生错误事件
    /// </summary>
    public event EventHandler<SocketError>? ErrorOccurred;

    protected override void OnConnected()
    {
        Connected?.Invoke(this, EventArgs.Empty);
        ReceiveAsync();
    }

    protected override void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        DataReceived?.Invoke(this, (endpoint, buffer, offset, size));
        ReceiveAsync();
    }

    protected override void OnError(SocketError error)
    {
        ErrorOccurred?.Invoke(this, error);
    }
}

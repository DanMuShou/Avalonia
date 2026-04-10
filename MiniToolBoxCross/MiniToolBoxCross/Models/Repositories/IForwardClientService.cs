using System;
using System.Net;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Repositories;

public interface IForwardClientService
{
    Action? OnErrorOccurred { get; set; }
    bool IsConfigured { get; }
    bool IsConnected { get; }
    bool Config(IPEndPoint endPoint);
    bool Connect();
    bool Disconnect();
    bool Reconnect();
    UdpEchoServer? AddForwardServer(IPEndPoint endPoint, ForwardTargetType forwardTargetType);
    bool RemoveForwardServer(Guid id);
    void SendData(byte[] buffer, long offset, long size);
}

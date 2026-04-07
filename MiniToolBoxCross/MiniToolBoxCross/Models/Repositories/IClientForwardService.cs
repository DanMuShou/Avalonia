using System;
using System.Net;
using System.Threading.Tasks;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Repositories;

public interface IClientForwardService
{
    bool IsConfigured { get; }
    bool IsConnected { get; }
    Task<bool> ConfigAsync(IPEndPoint endPoint);
    Task<bool> ConnectAsync();
    Task<bool> DisconnectAsync();
    Task<bool> ReconnectAsync();
    Task<EchoServer?> AddForwardServerAsync(
        IPEndPoint endPoint,
        ForwardTargetType forwardTargetType
    );
    Task<bool> RemoveForwardServerAsync(Guid serverId);

    // void SendToForwardServers(string forwardTargetType, byte[] data, long offset, long size);
    // void SendToForwardServers(string forwardTargetType, string message);
    Task<bool> DisposeAsync();
}

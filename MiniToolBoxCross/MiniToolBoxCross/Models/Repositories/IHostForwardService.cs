using System;
using System.Net;
using System.Threading.Tasks;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Repositories;

public interface IHostForwardService
{
    bool IsConfigured { get; }
    bool IsStarted { get; }
    Task<bool> ConfigAsync(IPEndPoint endPoint);
    Task<bool> StartAsync();
    Task<bool> StopAsync();
    Task<bool> RestartAsync();
    EchoClient? AddForwardClientAsync(IPEndPoint endPoint, ForwardTargetType forwardTargetType);
    bool RemoveForwardClientAsync(Guid clientId);
    void Send(string forwardTargetType, string message);
    Task<bool> DisposeAsync();
}

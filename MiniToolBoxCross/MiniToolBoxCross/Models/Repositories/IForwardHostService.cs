using System;
using System.Net;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Services;

namespace MiniToolBoxCross.Models.Repositories;

public interface IForwardHostService
{
    Action? OnErrorOccurred { get; set; }
    bool IsConfigured { get; }
    bool IsStarted { get; }
    bool Config(IPEndPoint endPoint);
    bool Start();
    bool Stop();
    bool Restart();
    UdpEchoClient? AddForwardClient(IPEndPoint endPoint, ForwardTargetType forwardTargetType);
    bool RemoveForwardClient(Guid id);
}

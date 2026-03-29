using System;
using System.Net;

namespace MiniToolBoxCross.Common.Messages.NetServer;

public record TcpServerConnectMessage(
    Guid Id,
    bool IsConnect,
    EndPoint? EndPoint = null,
    DateTime? ConnectedDate = null
);

using System.Net;

namespace MiniToolBoxCross.Common.Messages.SocketService;

public record SocketServiceConfigureMessage(
    string Name,
    IPAddress ServerIpAddress,
    int ServerPort,
    IPAddress LocalIpAddress,
    int LocalPort
);

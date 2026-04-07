using System.Net;

namespace MiniToolBoxCross.Common.Messages.Socket;

public record SocketServiceConfigureMessage(
    string Name,
    IPAddress ServerIpAddress,
    int ServerPort,
    IPAddress LocalIpAddress,
    int LocalPort
);

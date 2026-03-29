using System.Net;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Common.Messages.SocketService;

public record SocketServiceConfigureMessage(
    SocketConfigureType Type,
    IPEndPoint Server,
    IPEndPoint? Local = null
);

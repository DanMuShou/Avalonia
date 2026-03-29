using System.Net;

namespace MiniToolBoxCross.Common.Messages.NetServer;

public record TcpServerRunStateMessage(bool IsRunning, IPEndPoint? IpEndPoint = null);

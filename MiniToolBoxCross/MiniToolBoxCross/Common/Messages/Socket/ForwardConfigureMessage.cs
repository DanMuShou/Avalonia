using System.Net;

namespace MiniToolBoxCross.Common.Messages.Socket;

public record ForwardConfigureMessage(string Name, IPAddress IpAddress, int Port);

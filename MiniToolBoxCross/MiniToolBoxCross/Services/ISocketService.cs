using System.Threading.Tasks;
using SuperSocket.Server.Abstractions;

namespace MiniToolBoxCross.Services;

public interface ISocketService : IServer
{
    Task BroadcastAsync(string clientKey, byte[] buffer, int offset, int size);
    Task BroadcastAsync(string message);
}

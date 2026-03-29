using System.Threading.Tasks;

namespace MiniToolBoxCross.Services;

public interface ISocketService
{
    public Task RegisterAuthAsync(SocketSession session);
    public Task UnregisterAuthAsync(SocketSession session);
}

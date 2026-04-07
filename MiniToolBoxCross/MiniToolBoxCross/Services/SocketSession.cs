using System.Threading.Tasks;
using SuperSocket.Connection;
using SuperSocket.Server;

namespace MiniToolBoxCross.Services;

public class SocketSession : AppSession
{
    protected override async ValueTask OnSessionConnectedAsync()
    {
        await base.OnSessionConnectedAsync();
    }

    protected override ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        return base.OnSessionClosedAsync(e);
    }
}

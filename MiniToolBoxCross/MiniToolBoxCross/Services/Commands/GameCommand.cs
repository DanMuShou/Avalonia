using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace MiniToolBoxCross.Services.Commands;

public class GameCommand : IAsyncCommand<SocketSession, TextPackageInfo>
{
    public ValueTask ExecuteAsync(
        SocketSession session,
        TextPackageInfo package,
        CancellationToken cancellationToken
    )
    {
        throw new System.NotImplementedException();
    }
}

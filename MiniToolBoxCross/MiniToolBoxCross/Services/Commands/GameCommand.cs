using System.Threading;
using System.Threading.Tasks;
using MiniToolBoxCross.Models.Repositories;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace MiniToolBoxCross.Services.Commands;

[Command(Key = "Game")]
public class GameCommand(IHostForwardService hostForwardService)
    : IAsyncCommand<SocketSession, StringPackageInfo>
{
    public async ValueTask ExecuteAsync(
        SocketSession session,
        StringPackageInfo package,
        CancellationToken cancellationToken
    )
    {
        if (package.Parameters is not { Length: 2 })
            return;
        var forwardTargetType = package.Parameters[0];
        var message = package.Parameters[1];
        hostForwardService.Send(forwardTargetType, message);
    }
}

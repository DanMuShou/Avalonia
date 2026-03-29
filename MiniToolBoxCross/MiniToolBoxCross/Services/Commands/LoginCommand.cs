using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Models.Entities;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace MiniToolBoxCross.Services.Commands;

[Command(Key = nameof(SocketCommandType.Login))]
public class LoginCommand : IAsyncCommand<SocketSession, StringPackageInfo>
{
    public async ValueTask ExecuteAsync(
        SocketSession session,
        StringPackageInfo package,
        CancellationToken cancellationToken
    )
    {
        if (session.IsAuthenticated)
            return;

        if (string.IsNullOrWhiteSpace(package.Body))
            return;

        var loginInfo = JsonSerializer.Deserialize<LoginRequest>(package.Body);
        await session.AuthenticateAsync(loginInfo);
    }
}

using System.Threading.Tasks;
using SuperSocket.Command;

namespace MiniToolBoxCross.Services.Commands;

public class AuthAsyncCommandFilterAttribute : AsyncCommandFilterAttribute
{
    public override async ValueTask OnCommandExecutedAsync(CommandExecutingContext commandContext)
    {
        await Task.Delay(0);
    }

    public override async ValueTask<bool> OnCommandExecutingAsync(
        CommandExecutingContext commandContext
    )
    {
        await Task.Delay(0);
        return commandContext.Session is SocketSession { IsAuthenticated: true };
    }
}

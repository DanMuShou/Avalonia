using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniToolBoxCross.Common.Helper;
using SuperSocket;
using SuperSocket.Connection;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;

namespace MiniToolBoxCross.Services;

public class SocketService<TStringPackageInfo>(
    IServiceProvider serviceProvider,
    IOptions<ServerOptions> serverOptions,
    ILogger<SocketService<TStringPackageInfo>> logger
) : SuperSocketService<TStringPackageInfo>(serviceProvider, serverOptions), ISocketService
{
    private readonly HashSet<IAppSession> _session = [];
    private readonly Lock _sessionLock = new();

    protected override async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        lock (_sessionLock)
            _session.Add(session);

        logger.LogInformation("已连接 [SessionID: {SessionID}]", session.SessionID);
        await base.OnSessionConnectedAsync(session);
    }

    protected override async ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
    {
        lock (_sessionLock)
            _session.Remove(session);

        logger.LogInformation(
            "会话已关闭 [SessionID: {SessionID} Reason: {Reason}]",
            session.SessionID,
            e.Reason
        );
        await base.OnSessionClosedAsync(session, e);
    }

    protected override ValueTask OnNewConnectionAccept(
        ListenOptions listenOptions,
        IConnection connection
    )
    {
        return base.OnNewConnectionAccept(listenOptions, connection);
    }

    protected override ValueTask<bool> OnSessionErrorAsync(
        IAppSession session,
        PackageHandlingException<TStringPackageInfo> exception
    )
    {
        logger.LogError(
            exception,
            "会话错误 [SessionID: {SessionID} Error: {Error}]",
            session.SessionID,
            exception.Message
        );
        return base.OnSessionErrorAsync(session, exception);
    }

    protected override ValueTask OnStartedAsync()
    {
        return base.OnStartedAsync();
    }

    protected override ValueTask OnStopAsync()
    {
        return base.OnStopAsync();
    }

    public async Task BroadcastAsync(string clientKey, byte[] buffer, int offset, int size)
    {
        var encoded = SocketHelper.StringPackInfoMessageEncoder(clientKey, buffer, offset, size);

        // 打印字节数组的十六进制值
        var hexString = string.Join(" ", encoded.Span.ToArray().Select(b => $"0x{b:X2}"));
        Console.WriteLine($"[BroadcastAsync] 编码后的字节: {hexString}");

        foreach (var session in _session)
        {
            try
            {
                await session.SendAsync(encoded);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BroadcastAsync] 发送失败: {ex.Message}");
            }
        }
    }

    public async Task BroadcastAsync(string message)
    {
        foreach (var session in _session)
        {
            try
            {
                await session.SendAsync("Test 1 Info\r\n"u8.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BroadcastAsync] 发送失败: {ex.Message}");
            }
        }
    }
}

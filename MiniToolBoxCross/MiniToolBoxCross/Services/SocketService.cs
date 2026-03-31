using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Services.Commands;
using Serilog;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace MiniToolBoxCross.Services;

public class SocketService<TStringPackageInfo>
    : SuperSocketService<TStringPackageInfo>,
        ISocketService
{
    private readonly ILogger<SocketService<TStringPackageInfo>> _logger;

    /// <summary>
    /// 未认证的 Session 字典
    /// </summary>
    private readonly ConcurrentDictionary<string, IAppSession> _unauthenticatedSessions = new();

    /// <summary>
    /// 已认证的 Session 字典
    /// </summary>
    private readonly ConcurrentDictionary<string, IAppSession> _authenticatedSessions = new();

    public SocketService(
        IServiceProvider serviceProvider,
        IOptions<ServerOptions> serverOptions,
        ILogger<SocketService<TStringPackageInfo>> logger
    )
        : base(serviceProvider, serverOptions)
    {
        _logger = logger;
    }

    protected override async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        _unauthenticatedSessions.TryAdd(session.SessionID, session);
        await base.OnSessionConnectedAsync(session);
    }

    protected override async ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
    {
        if (session is SocketSession { IsAuthenticated: true })
        {
            _authenticatedSessions.TryRemove(session.SessionID, out _);
        }
        else
        {
            _unauthenticatedSessions.TryRemove(session.SessionID, out _);
        }

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
        _logger.LogError(
            exception,
            "会话错误 [SessionID: {SessionID} Error: {Error}]",
            session.SessionID,
            exception.Message
        );
        return base.OnSessionErrorAsync(session, exception);
    }

    protected override ValueTask OnStartedAsync()
    {
        _logger.LogInformation("Socket 服务已启动");
        return base.OnStartedAsync();
    }

    protected override ValueTask OnStopAsync()
    {
        _logger.LogInformation("Socket 服务已停止");
        _unauthenticatedSessions.Clear();
        _authenticatedSessions.Clear();
        return base.OnStopAsync();
    }

    public async Task RegisterAuthAsync(SocketSession session)
    {
        await Task.Delay(0);
        _unauthenticatedSessions.TryRemove(session.SessionID, out var _);
        _authenticatedSessions.TryAdd(session.SessionID, session);
        _logger.LogInformation(
            "Session 已认证 [SessionID: {SessionID}, User: {User}]",
            session.SessionID,
            session.LoginInfo?.Name
        );
    }

    public async Task UnregisterAuthAsync(SocketSession session)
    {
        await Task.Delay(0);
        _authenticatedSessions.TryRemove(session.SessionID, out _);
        _logger.LogInformation(
            "Session 已取消认证 [SessionID: {SessionID}, User: {User}]",
            session.SessionID,
            session.LoginInfo?.Name
        );
    }

    // public static IServer BuildSuperSocket(
    //     List<ListenOptions> listenOptionsList,
    //     bool useUdp = false
    // )
    // {
    //     var socketHostBuilder = SuperSocketHostBuilder
    //         .Create<StringPackageInfo, CommandLinePipelineFilter>()
    //         .UseHostedService<SocketService<StringPackageInfo>>()
    //         .UseSession<SocketSession>()
    //         .UseCommand(
    //             (commandOptions) =>
    //             {
    //                 commandOptions.AddCommand<LoginCommand>();
    //                 // commandOptions.AddGlobalCommandFilter<AuthAsyncCommandFilterAttribute>();
    //             }
    //         )
    //         .UseInProcSessionContainer()
    //         .ConfigureSuperSocket(options =>
    //             listenOptionsList.ForEach(o => options.AddListener(o))
    //         );
    //
    //     if (useUdp)
    //     {
    //         socketHostBuilder.UseUdp();
    //     }
    //
    //     var hostBuilder = socketHostBuilder
    //         .ConfigureServices(services =>
    //         {
    //             services.AddSingleton<CrossSetting>();
    //             services.AddLogging(builder =>
    //             {
    //                 builder.SetMinimumLevel(LogLevel.Debug);
    //             });
    //         })
    //         .ConfigureLogging(
    //             (_, loggingBuilder) =>
    //             {
    //                 loggingBuilder.ClearProviders();
    //                 loggingBuilder.AddSerilog(dispose: true);
    //             }
    //         );
    //
    //     return hostBuilder.BuildAsServer();
    // }
}

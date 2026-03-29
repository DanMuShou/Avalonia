using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Services;
using MiniToolBoxCross.Services.Commands;
using Serilog;
using SuperSocket.Command;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;

namespace MiniToolBoxCross.Common.Extensions;

/// <summary>
/// SuperSocket 扩展方法
/// </summary>
public static class SuperSocketExtensions
{
    /// <summary>
    /// 配置并使用 SuperSocket 服务器
    /// </summary>
    /// <returns>配置后的 ISuperSocketHostBuilder</returns>
    public static IServer CreateSuperSocket(
        List<ListenOptions> listenOptionsList,
        bool useUdp = false
    )
    {
        var socketHostBuilder = SuperSocketHostBuilder
            .Create<StringPackageInfo, CommandLinePipelineFilter>()
            .UseHostedService<SocketService<StringPackageInfo>>()
            .UseSession<SocketSession>()
            .UseCommand(
                (commandOptions) =>
                {
                    commandOptions.AddCommand<LoginCommand>();
                    // commandOptions.AddGlobalCommandFilter<AuthAsyncCommandFilterAttribute>();
                }
            )
            .UseInProcSessionContainer()
            .ConfigureSuperSocket(options =>
                listenOptionsList.ForEach(o => options.AddListener(o))
            );

        if (useUdp)
        {
            socketHostBuilder.UseUdp();
        }

        var hostBuilder = socketHostBuilder
            .ConfigureServices(services =>
            {
                services.AddSingleton<CrossSetting>();
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .ConfigureLogging(
                (_, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(dispose: true);
                }
            );

        return hostBuilder.BuildAsServer();
    }
}

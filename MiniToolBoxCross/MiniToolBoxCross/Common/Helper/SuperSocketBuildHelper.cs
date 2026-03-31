using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace MiniToolBoxCross.Common.Helper;

public class SuperSocketBuildHelper
{
    public static IServer BuildGameSuperSocket(List<ListenOptions> listenOptionsList, bool useUdp)
    {
        var socketHostBuilder = SuperSocketHostBuilder
            .Create<TextPackageInfo, TransparentPipelineFilter<TextPackageInfo>>()
            .UseHostedService<SocketService<TextPackageInfo>>()
            .UseSession<SocketSession>()
            .UseInProcSessionContainer()
            .UsePackageHandler(
                (session, package) =>
                {
                    Console.WriteLine(package.Text);
                    return ValueTask.CompletedTask;
                }
            )
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

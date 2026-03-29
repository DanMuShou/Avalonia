using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace MiniToolBoxCross.Common.Extensions;

/// <summary>
/// Serilog 日志配置扩展方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 配置 Serilog 日志输出，支持自定义应用名称
    /// </summary>
    public static IHostBuilder UseOnlySerilog(this IHostBuilder hostBuilder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug,
                standardErrorFromLevel: LogEventLevel.Warning
            )
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
            .CreateLogger();

        hostBuilder.ConfigureLogging(
            (_, logging) =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            }
        );

        return hostBuilder;
    }
}

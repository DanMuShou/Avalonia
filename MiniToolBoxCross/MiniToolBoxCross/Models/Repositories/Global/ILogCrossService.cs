using System;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Models.Repositories.Global;

public interface ILogCrossService : ILogger
{
    void Log(string message);

    void Debug(string message);

    void Information(string message);

    void Warning(string message);
    void Error(string message);
    void Error(Exception exception);
    void Log(string message, LogLevel level);

    void Log(string message, LogLevel level, OsPlatformType os);
    void Log(string message, Exception? exception, LogLevel level, OsPlatformType os);
}

/// <summary>
/// ILogCrossService 扩展方法
/// </summary>
public static class LogCrossServiceExtensions
{
    public static void LogInfo(this ILogCrossService logger, string message)
    {
        logger.Information(message);
    }

    public static void LogDebug(this ILogCrossService logger, string message)
    {
        logger.Debug(message);
    }

    public static void LogWarning(this ILogCrossService logger, string message)
    {
        logger.Warning(message);
    }

    public static void LogError(this ILogCrossService logger, string message)
    {
        logger.Error(message);
    }

    public static void LogSuccess(this ILogCrossService logger, string message)
    {
        logger.Information($"✓ {message}");
    }
}

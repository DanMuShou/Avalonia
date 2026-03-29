using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Models.Repositories.Global;

namespace MiniToolBoxCross.Models.Services.Global;

public class LogCrossService : ILogCrossService
{
    private readonly ILogger _logger;
    private readonly ConcurrentStack<IDisposable> _scopes = new();

    public LogCrossService(ILogger<LogCrossService> logger)
    {
        _logger = logger;
    }

    public void Log(string message)
    {
        Log(message, LogLevel.Information);
    }

    public void Log(string message, LogLevel level)
    {
        LogInternal(message, null, level);
    }

    public void Log(string message, LogLevel level, OsPlatformType os) =>
        Log(message, null, level, os);

    public void Log(string message, Exception? exception, LogLevel level, OsPlatformType os)
    {
        // 目前所有平台都使用相同的日志记录方式
        LogInternal(message, exception, level);
    }

    public void Debug(string message) => Log(message, LogLevel.Debug);

    public void Information(string message) => Log(message, LogLevel.Information);

    public void Warning(string message) => Log(message, LogLevel.Warning);

    public void Error(string message) => Log(message, LogLevel.Error);

    public void Error(Exception exception) =>
        LogInternal(exception.Message, exception, LogLevel.Error);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        LogInternal(message, exception, logLevel);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        var scope = _logger.BeginScope(state);
        if (scope is not null)
        {
            _scopes.Push(scope);
        }
        return new ScopeWrapper(() =>
        {
            if (_scopes.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        });
    }

    private void LogInternal(string message, Exception? exception, LogLevel level)
    {
        if (exception != null)
        {
            _logger.Log(level, exception, message);
        }
        else
        {
            _logger.Log(level, message);
        }
    }

    private sealed class ScopeWrapper(Action disposeAction) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                disposeAction();
                _disposed = true;
            }
        }
    }
}

using System;
using Avalonia.Controls.Notifications;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Models.Repositories;

namespace MiniToolBoxCross.Models.Services;

/// <summary>
/// 弹窗通知服务实现
/// 发送弹窗时自动记录到日志系统
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly CrossSystemFunc _crossSystemFunc;

    public NotificationService(CrossSystemFunc crossSystemFunc, ILogger<NotificationService> logger)
    {
        _crossSystemFunc = crossSystemFunc;
        _logger = logger;
    }

    public void ShowInformation(string title, string message)
    {
        Show(title, message, NotificationType.Information);
    }

    public void ShowSuccess(string title, string message)
    {
        Show(title, message, NotificationType.Success);
    }

    public void ShowWarning(string title, string message)
    {
        Show(title, message, NotificationType.Warning);
    }

    public void ShowError(string title, string message)
    {
        Show(title, message, NotificationType.Error);
    }

    public void Show(string title, string message, NotificationType type)
    {
        // 根据通知类型记录不同级别的日志（使用延迟计算避免不必要的字符串构造）
        switch (type)
        {
            case NotificationType.Information:
                _logger.LogInformation("[{Title}] {Message}", title, message);
                break;
            case NotificationType.Success:
                _logger.LogInformation("✓ [{Title}] {Message}", title, message);
                break;
            case NotificationType.Warning:
                _logger.LogWarning("[{Title}] {Message}", title, message);
                break;
            case NotificationType.Error:
                _logger.LogError("[{Title}] {Message}", title, message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        _crossSystemFunc.ShowNotification(new Ursa.Controls.Notification(title, message), type);
    }
}

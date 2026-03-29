using Avalonia.Controls.Notifications;

namespace MiniToolBoxCross.Common.Messages.Notification;

/// <summary>
/// 弹窗通知消息
/// </summary>
/// <param name="Title">标题</param>
/// <param name="Message">消息内容</param>
/// <param name="NotificationType">通知类型</param>
public record NotificationMessage(
    string Title,
    string Message,
    NotificationType NotificationType = NotificationType.Information
);

using Avalonia.Controls.Notifications;

namespace MiniToolBoxCross.Models.Repositories;

/// <summary>
/// 弹窗通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 显示信息通知
    /// </summary>
    void ShowInformation(string title, string message);

    /// <summary>
    /// 显示成功通知
    /// </summary>
    void ShowSuccess(string title, string message);

    /// <summary>
    /// 显示警告通知
    /// </summary>
    void ShowWarning(string title, string message);

    /// <summary>
    /// 显示错误通知
    /// </summary>
    void ShowError(string title, string message);

    /// <summary>
    /// 显示自定义通知
    /// </summary>
    void Show(string title, string message, NotificationType type);
}

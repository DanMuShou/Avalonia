using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;

namespace MiniToolBoxCross.Common.Global;

public class CrossSystemFunc
{
    private TopLevel? TopLevel { get; set; }
    private Ursa.Controls.WindowNotificationManager? NotificationManager { get; set; }

    public void LazyInitialize()
    {
        if (Application.Current is null)
            throw new InvalidOperationException("Application.Current is null");

        if (Application.Current.ApplicationLifetime is not null)
            TopLevel =
                Application.Current.ApplicationLifetime switch
                {
                    IClassicDesktopStyleApplicationLifetime desktop => TopLevel.GetTopLevel(
                        desktop.MainWindow
                    ),
                    ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(
                        singleView.MainView
                    ),
                    _ => throw new InvalidOperationException(
                        "Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime or ISingleViewApplicationLifetime"
                    ),
                } ?? throw new InvalidOperationException("TopLevel is null");

        NotificationManager = Ursa.Controls.WindowNotificationManager.TryGetNotificationManager(
            TopLevel,
            out var manager
        )
            ? manager
            : new Ursa.Controls.WindowNotificationManager(TopLevel);
    }

    public async Task<string> GetClipboardTextAsync()
    {
        if (TopLevel?.Clipboard is null)
            return "";
        var data = await TopLevel.Clipboard.TryGetTextAsync();
        return data ?? "";
    }

    public async Task SetClipboardText(string text)
    {
        if (TopLevel?.Clipboard is null)
            return;
        await TopLevel.Clipboard.SetTextAsync(text);
    }

    public void ShowNotification(
        Ursa.Controls.Notification notification,
        NotificationType type,
        bool showIcon = true,
        bool showClose = true
    )
    {
        NotificationManager?.Show(notification, type, TimeSpan.FromSeconds(3), showIcon, showClose);
    }
}

using Microsoft.Extensions.DependencyInjection;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Models.Services;
using MiniToolBoxCross.ViewModels;
using MiniToolBoxCross.ViewModels.Pages;
using MiniToolBoxCross.ViewModels.UserControls;

namespace MiniToolBoxCross.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // 注册通知服务
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IForwardHostService, ForwardHostService>();
        services.AddSingleton<IForwardClientService, ForwardClientService>();
        // services.AddSingleton<IForwardHostService, ForwardHostService>();
        // services.AddSingleton<IForwardClientService, ForwardClientService>();

        // 注册设置
        services.AddSingleton<CrossSetting>();
        services.AddSingleton<CrossSystemFunc>();

        // 注册视图模型
        services.AddTransient<MainViewModel>();
        services.AddTransient<ForwardHostViewModel>();
        services.AddTransient<ForwardClientViewModel>();

        // 注册用户控件
        services.AddTransient<LogBoxViewModel>();
    }
}

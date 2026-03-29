using Microsoft.Extensions.DependencyInjection;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Models.Repositories.Global;
using MiniToolBoxCross.Models.Services.Global;
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

        // 注册设置
        services.AddSingleton<CrossSetting>();
        services.AddSingleton<CrossSystemFunc>();

        // 注册视图模型
        services.AddTransient<MainViewModel>();
        services.AddTransient<SocketServerViewModel>();
        services.AddTransient<SocketClientViewModel>();

        // 注册用户控件
        services.AddTransient<LogBoxViewModel>();
    }
}

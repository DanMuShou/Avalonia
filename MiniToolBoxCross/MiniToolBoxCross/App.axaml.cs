using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniToolBoxCross.Common.Extensions;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.ViewModels;
using MiniToolBoxCross.Views;

namespace MiniToolBoxCross;

public partial class App : Application
{
    private readonly IHost _host;
    private IServiceProvider Services => _host.Services;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .UseOnlySerilog()
            .ConfigureServices((_, services) => services.AddServices())
            .Build();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = Services.GetRequiredService<MainViewModel>(),
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = Services.GetRequiredService<MainViewModel>(),
                };
                break;
        }

        var crossSystemFunc = Services.GetRequiredService<CrossSystemFunc>();
        crossSystemFunc.LazyInitialize();

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

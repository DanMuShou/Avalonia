using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniToolBoxCross.Common.Extensions;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Repositories.Global;
using SuperSocket.Server.Abstractions;

namespace MiniToolBoxCross.ViewModels.UserControls;

public partial class SocketServerCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private IPAddress _ipAddress = IPAddress.None;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartServerCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartServerCommand))]
    private bool _isBusy;

    private bool CanStartServer() => !IsRunning && !IsBusy;

    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;
    private IServer? _server;

    public SocketServerCardViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
    }

    private async Task<IServer> InitSocket(IPAddress ipAddress, int port)
    {
        if (_server is null)
            return SuperSocketExtensions.CreateSuperSocket(
                [new ListenOptions { Ip = ipAddress.ToString(), Port = port }]
            );

        await _server.StopAsync();
        _server.Dispose();
        _server = null;
        return SuperSocketExtensions.CreateSuperSocket(
            [new ListenOptions { Ip = ipAddress.ToString(), Port = port }]
        );
    }

    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServer()
    {
        if (IsRunning || IsBusy)
            return;

        var ipAddress = SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6);
        if (!ipAddress.Contains(IpAddress))
        {
            _notificationService.ShowError("验证失败", "请选择有效的 IP 地址");
            return;
        }

        IsBusy = true;
        try
        {
            _server = await InitSocket(IpAddress, Port);
            await _server.StartAsync();
            IsRunning = true;

            _notificationService.ShowSuccess("服务器启动", $"已成功启动，监听 {IpAddress}:{Port}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("启动失败", $"无法启动服务器：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanStopServer() => IsRunning && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServer()
    {
        if (!IsRunning || IsBusy || _server is null)
            return;

        IsBusy = true;
        try
        {
            await _server.StopAsync();
            _server.Dispose();
            _server = null;
            IsRunning = false;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("停止服务器失败", $"无法停止服务器：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRestartServer() => IsRunning && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRestartServer))]
    private async Task RestartServer()
    {
        if (!IsRunning || IsBusy || _server is null)
            return;
        IsBusy = true;
        try
        {
            _server = await InitSocket(IpAddress, Port);
            await _server.StartAsync();
            IsRunning = true;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("重启服务器失败", $"无法重启服务器：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyIp()
    {
        await _crossSystemFunc.SetClipboardText(new IPEndPoint(IpAddress, Port).ToString());
    }
}

using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Messages.SocketService;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories.Global;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.ViewModels.UserControls;
using MiniToolBoxCross.Views.Dialogs;
using Ursa.Controls;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private SocketConfigureType _serverType = SocketConfigureType.PortForwarding;

    public ObservableCollection<IPAddress> IpAddressList { get; set; }
    public ObservableCollection<SocketServerModel> SocketServerModels { get; set; }

    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;

    public SocketServerViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;

        SocketServerModels = [new SocketServerModel()];

        RegisterMessage();
    }

    // private void Refresh()
    // {
    //     var ipAddressList = SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6);
    //
    //     IpAddressList = new ObservableCollection<IPAddress>(ipAddressList);
    //
    //     ServerIp = ipAddressList.FirstOrDefault() ?? IPAddress.None;
    //     var validServerPort = SocketHelper.CheckPort(ServerPort);
    //     if (validServerPort.HasValue)
    //     {
    //         ServerPort = validServerPort.Value;
    //     }
    //     else
    //     {
    //         _notificationService.ShowError("配置错误", "没有找到可用的网络端口");
    //         ServerPort = 0;
    //     }
    //     LocalIp = IPAddress.Loopback;
    //     var validLocalPort = SocketHelper.CheckPort(LocalPort);
    //     if (validLocalPort.HasValue)
    //     {
    //         LocalPort = validLocalPort.Value;
    //     }
    //     else
    //     {
    //         _notificationService.ShowError("配置错误", "没有找到可用的网络端口");
    //         LocalPort = 0;
    //     }
    // }

    private void RegisterMessage()
    {
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<SocketServiceConfigureMessage>>(
            this,
            (_, message) =>
            {
                var value = message.Value;
            }
        );
    }

    [RelayCommand]
    private async Task OpenConfigureServerDialog()
    {
        var options = new OverlayDialogOptions()
        {
            Title = "配置服务器",
            FullScreen = false,
            IsCloseButtonVisible = true,
            CanDragMove = true,
            CanResize = true,
        };

        await OverlayDialog.ShowCustomModal<
            SocketServerConfigureDialog,
            SocketServerConfigureDialogViewModel,
            object
        >(new SocketServerConfigureDialogViewModel(ServerType), options: options);
    }

    // [RelayCommand(CanExecute = nameof(CanStartServer))]
    // private async Task StartServer()
    // {
    //     if (IsRunning || IsBusy)
    //         return;
    //
    //     var ipAddress = SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6);
    //     if (!ipAddress.Contains(IpAddress))
    //     {
    //         _notificationService.ShowError("验证失败", "请选择有效的 IP 地址");
    //         return;
    //     }
    //
    //     IsBusy = true;
    //     try
    //     {
    //         _server = await InitSocket(IpAddress, Port);
    //         await _server.StartAsync();
    //         IsRunning = true;
    //
    //         _notificationService.ShowSuccess("服务器启动", $"已成功启动，监听 {IpAddress}:{Port}");
    //     }
    //     catch (Exception ex)
    //     {
    //         _notificationService.ShowError("启动失败", $"无法启动服务器：{ex.Message}");
    //     }
    //     finally
    //     {
    //         IsBusy = false;
    //     }
    // }
    //
    // private bool CanStopServer() => IsRunning && !IsBusy;
    //
    // [RelayCommand(CanExecute = nameof(CanStopServer))]
    // private async Task StopServer()
    // {
    //     if (!IsRunning || IsBusy || _server is null)
    //         return;
    //
    //     IsBusy = true;
    //     try
    //     {
    //         await _server.StopAsync();
    //         _server.Dispose();
    //         _server = null;
    //         IsRunning = false;
    //     }
    //     catch (Exception ex)
    //     {
    //         _notificationService.ShowError("停止服务器失败", $"无法停止服务器：{ex.Message}");
    //     }
    //     finally
    //     {
    //         IsBusy = false;
    //     }
    // }
    //
    // private bool CanRestartServer() => IsRunning && !IsBusy;
    //
    // [RelayCommand(CanExecute = nameof(CanRestartServer))]
    // private async Task RestartServer()
    // {
    //     if (!IsRunning || IsBusy || _server is null)
    //         return;
    //     IsBusy = true;
    //     try
    //     {
    //         _server = await InitSocket(IpAddress, Port);
    //         await _server.StartAsync();
    //         IsRunning = true;
    //     }
    //     catch (Exception ex)
    //     {
    //         _notificationService.ShowError("重启服务器失败", $"无法重启服务器：{ex.Message}");
    //     }
    //     finally
    //     {
    //         IsBusy = false;
    //     }
    // }
    //
    [RelayCommand]
    private async Task CopyIp(SocketServerModel? model)
    {
        await _crossSystemFunc.SetClipboardText(
            new IPEndPoint(model.IpAddress, model.Port).ToString()
        );
    }
}

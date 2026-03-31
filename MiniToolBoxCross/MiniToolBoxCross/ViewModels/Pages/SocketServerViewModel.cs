using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Extensions;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Common.Messages.SocketService;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories.Global;
using MiniToolBoxCross.Services;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;
using SuperSocket.Client;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions;
using Ursa.Controls;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private SocketConfigureType _serverType = SocketConfigureType.PortForwarding;

    public ObservableCollection<SocketServerModel> SocketServerModels { get; set; }
    private IServer? GameSocketServer { get; set; }
    private IEasyClient<TextPackageInfo> EasyClient { get; set; }
    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;

    public SocketServerViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        SocketServerModels = [];
        RegisterMessage();

        EasyClient = new EasyClient<TextPackageInfo>(
            new TransparentPipelineFilter<TextPackageInfo>()
        );

        Console.WriteLine(SocketHelper.GetIpv4AddressList().First());
        EasyClient.PackageHandler += (sender, package) =>
        {
            Console.WriteLine(package.Text);
            return new ValueTask();
        };
        EasyClient.ConnectAsync(new IPEndPoint(SocketHelper.GetIpv4AddressList().First(), 7689));
        Console.WriteLine(SocketHelper.GetIpv4AddressList().First());

        EasyClient.StartReceive();
    }

    private void RegisterMessage()
    {
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<SocketServiceConfigureMessage>>(
            this,
            (_, message) =>
            {
                var value = message.Value;
                SocketServerModels.Add(
                    new SocketServerModel(
                        Guid.NewGuid(),
                        value.Name,
                        new IPEndPoint(value.ServerIpAddress, value.ServerPort),
                        new IPEndPoint(value.LocalIpAddress, value.LocalPort)
                    )
                );
            }
        );
    }

    private async Task<IServer> BuildGameSocketServer(
        IServer? gameSocketServer,
        IPAddress serverIpAddress,
        int serverPort,
        IPAddress localIpAddress,
        int localPort
    )
    {
        if (gameSocketServer is not null)
        {
            await gameSocketServer.StopAsync();
            gameSocketServer.Dispose();
        }

        return SuperSocketBuildHelper.BuildGameSuperSocket(
            [
                new ListenOptions { Ip = serverIpAddress.ToString(), Port = serverPort },
                new ListenOptions { Ip = localIpAddress.ToString(), Port = localPort },
            ],
            false
        );
    }

    private bool CheckServerModelConfig(SocketServerModel model)
    {
        var serverIpEndPort = model.ServerIpEndPoint;
        if (!SocketHelper.GetIpv6AddressList().Contains(serverIpEndPort.Address))
        {
            _notificationService.ShowError("配置错误", "请刷新服务器地址");
            return false;
        }

        var localIpEndPort = model.LocalIpEndPoint;
        if (!SocketHelper.GetIpv4AddressList().Contains(localIpEndPort.Address))
        {
            _notificationService.ShowError("配置错误", "请刷新本地地址");
            return false;
        }

        if (serverIpEndPort.Port == localIpEndPort.Port)
        {
            _notificationService.ShowError("配置错误", "请选择不同的端口");
            return false;
        }

        var validServerPort = SocketHelper.CheckPort(serverIpEndPort.Port);
        var validLocalPort = SocketHelper.CheckPort(localIpEndPort.Port);
        if (!validServerPort.HasValue || !validLocalPort.HasValue)
        {
            _notificationService.ShowError("配置错误", "没有找到可用的网络端口");
            return false;
        }

        model.ServerIpEndPoint = new IPEndPoint(serverIpEndPort.Address, validServerPort.Value);
        model.LocalIpEndPoint = new IPEndPoint(localIpEndPort.Address, validLocalPort.Value);
        return true;
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

    [RelayCommand]
    private async Task StartPortForwardingServer()
    {
        var model = SocketServerModels.FirstOrDefault();
        if (model is null || model.IsRunning || model.IsBusy)
            return;
        if (!CheckServerModelConfig(model))
            return;

        model.IsBusy = true;
        try
        {
            GameSocketServer = await BuildGameSocketServer(
                GameSocketServer,
                model.ServerIpEndPoint.Address,
                model.ServerIpEndPoint.Port,
                model.LocalIpEndPoint.Address,
                model.LocalIpEndPoint.Port
            );
            await GameSocketServer.StartAsync();
            model.IsRunning = true;
            _notificationService.ShowSuccess(
                "服务器启动",
                $"[{model.Name}]已成功启动，监听 {model.ServerIpEndPoint.Address}:{model.ServerIpEndPoint.Port}"
            );
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                "启动失败",
                $"[{model.Name}]无法启动服务器：{ex.Message}"
            );
        }
        finally
        {
            model.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StopPortForwardingServer()
    {
        var model = SocketServerModels.FirstOrDefault();
        if (model is null || !model.IsRunning || model.IsBusy)
            return;
        if (GameSocketServer is null)
            return;

        model.IsBusy = true;
        try
        {
            await GameSocketServer.StopAsync();
            GameSocketServer.Dispose();
            model.IsRunning = false;
            _notificationService.ShowSuccess("服务器停止", $"[{model.Name}]已成功停止");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                "停止失败",
                $"[{model.Name}]无法停止服务器：{ex.Message}"
            );
        }
        finally
        {
            model.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestartPortForwardingServer()
    {
        var model = SocketServerModels.FirstOrDefault();
        if (model is null || model.IsBusy)
            return;
        if (GameSocketServer is null)
            return;
        if (!CheckServerModelConfig(model))
            return;

        model.IsBusy = true;
        try
        {
            GameSocketServer = await BuildGameSocketServer(
                GameSocketServer,
                model.ServerIpEndPoint.Address,
                model.ServerIpEndPoint.Port,
                model.LocalIpEndPoint.Address,
                model.LocalIpEndPoint.Port
            );
            model.IsRunning = true;
            _notificationService.ShowSuccess(
                "服务器启动",
                $"[{model.Name}]已成功启动，监听 {model.ServerIpEndPoint.Address}:{model.ServerIpEndPoint.Port}"
            );
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                "启动失败",
                $"[{model.Name}]无法启动服务器：{ex.Message}"
            );
        }
        finally
        {
            model.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyIp(Guid? socketServerId)
    {
        var model = SocketServerModels.FirstOrDefault(x => x.Id == socketServerId);
        if (model is null)
            return;
        await _crossSystemFunc.SetClipboardText(model.ServerIpEndPoint.ToString());
        _notificationService.ShowSuccess("复制成功", $"已复制 {model.ServerIpEndPoint} 到剪贴板");
    }
}

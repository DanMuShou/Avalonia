using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Common.Messages.SocketService;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories.Global;
using MiniToolBoxCross.Services;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;
using SuperSocket.Server.Abstractions;
using Ursa.Controls;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private SocketConfigureType _serverType = SocketConfigureType.PortForwarding;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private IPAddress _serverIpAddress;

    [ObservableProperty]
    private int _serverPort;

    public ObservableCollection<ListenerModel> ListenerModels { get; set; }
    public ObservableCollection<LocalSocketModel> LocalSocketModels { get; set; }
    private IServer? ServerSocket { get; set; }
    private Dictionary<Guid, EchoClient> EchoClients { get; set; }
    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;

    public SocketServerViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        ListenerModels = [];
        LocalSocketModels = [];
        EchoClients = [];
        RegisterMessage();
    }

    private void RegisterMessage()
    {
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<SocketServiceConfigureMessage>>(
            this,
            (_, message) =>
            {
                var value = message.Value;
                ListenerModels.Add(
                    new ListenerModel(
                        Guid.NewGuid(),
                        value.Name,
                        new IPEndPoint(value.ServerIpAddress, value.ServerPort)
                    )
                );
                LocalSocketModels.Add(
                    new LocalSocketModel(
                        Guid.NewGuid(),
                        value.Name,
                        "",
                        new IPEndPoint(value.LocalIpAddress, value.LocalPort)
                    )
                );
            }
        );
    }

    private bool CheckSocketConfig()
    {
        foreach (var listenerModel in ListenerModels)
        {
            var listenerIpEndPort = listenerModel.ListenerNetIpEndPoint;
            if (!SocketHelper.GetIpv6AddressList().Contains(listenerIpEndPort.Address))
            {
                _notificationService.ShowError(
                    "配置错误",
                    $"{listenerModel.Name} 请刷新服务器地址"
                );
                return false;
            }

            var validServerPort = SocketHelper.CheckPort(listenerIpEndPort.Port);
            if (!validServerPort.HasValue)
            {
                _notificationService.ShowError(
                    "配置错误",
                    $"{listenerModel.Name} 没有找到可用的网络端口"
                );
                return false;
            }
            listenerModel.ListenerNetIpEndPoint = new IPEndPoint(
                listenerIpEndPort.Address,
                validServerPort.Value
            );
        }

        foreach (var localModel in LocalSocketModels)
        {
            var localIpEndPort = localModel.LocalIpEndPoint;
            if (!SocketHelper.GetIpv4AddressList().Contains(localIpEndPort.Address))
            {
                _notificationService.ShowError("配置错误", $"{localModel.Name} 请刷新本地地址");
                return false;
            }

            var validLocalPort = SocketHelper.CheckPort(localIpEndPort.Port);
            if (!validLocalPort.HasValue)
            {
                _notificationService.ShowError(
                    "配置错误",
                    $"{localModel.Name} 没有找到可用的本地端口"
                );
                return false;
            }
            localModel.LocalIpEndPoint = new IPEndPoint(
                localIpEndPort.Address,
                validLocalPort.Value
            );
        }
        return true;
    }

    private EchoClient BuildEchoClient(IPEndPoint endPoint, EchoClient? oldClient)
    {
        if (oldClient is not null)
        {
            oldClient.Disconnect();
            oldClient.Dispose();
        }

        return new EchoClient(endPoint);
    }

    [RelayCommand]
    private async Task Start()
    {
        if (IsRunning || IsBusy)
            return;

        if (!CheckSocketConfig())
            return;

        await RebuildSocket();

        IsBusy = true;
        try
        {
            await ServerSocket!.StartAsync();
            EchoClients
                .Values.ToList()
                .ForEach(client =>
                {
                    client.Connected += (sender, args) =>
                    {
                        client.Send("Hello World");
                        Console.WriteLine($"已连接至 {client.Endpoint}");
                    };
                    client.Disconnected += (sender, args) =>
                    {
                        Console.WriteLine($"已断开连接 {client.Endpoint}");
                    };
                    client.DataReceived += (sender, args) =>
                    {
                        var message = Encoding.UTF8.GetString(
                            args.buffer,
                            (int)args.offset,
                            (int)args.size
                        );
                        Console.WriteLine($"收到消息：{message}");
                    };
                    client.ErrorOccurred += (sender, args) =>
                    {
                        Console.WriteLine($"{client.Endpoint} 发生错误");
                    };
                    client.Connect();
                });
            IsRunning = true;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("启动失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Stop()
    {
        if (!IsRunning || IsBusy || ServerSocket is null)
            return;

        IsBusy = true;
        try
        {
            await ServerSocket.StopAsync();
            EchoClients
                .Values.ToList()
                .ForEach(client =>
                {
                    client.Disconnect();
                    client.Dispose();
                });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("停止失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Restart()
    {
        if (!IsRunning || IsBusy)
            return;
        if (ServerSocket is null)
            return;
        if (!CheckSocketConfig())
            return;

        await RebuildSocket();

        IsBusy = true;
        try
        {
            await ServerSocket.StartAsync();
            EchoClients
                .Values.ToList()
                .ForEach(client =>
                {
                    client.Connect();
                    client.ReceiveAsync();
                });
            IsRunning = true;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("重启失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
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
    private async Task CopyIp(Guid? socketServerId)
    {
        var model = ListenerModels.FirstOrDefault(x => x.Id == socketServerId);
        if (model is null)
            return;
        await _crossSystemFunc.SetClipboardText(model.ListenerNetIpEndPoint.ToString());
        _notificationService.ShowSuccess(
            "复制成功",
            $"已复制 {model.ListenerNetIpEndPoint} 到剪贴板"
        );
    }

    private async Task RebuildSocket()
    {
        ServerSocket = await SocketHelper.BuildSocketServer(
            GetListenOptionsFromModel(),
            ServerSocket
        );

        foreach (var localModel in LocalSocketModels)
        {
            EchoClients.TryGetValue(localModel.Id, out var echoClient);
            var textIp = IPEndPoint.Parse("127.0.0.1:3333");
            echoClient = BuildEchoClient(textIp, echoClient);
            EchoClients[localModel.Id] = echoClient;
        }
    }

    private List<ListenOptions> GetListenOptionsFromModel()
    {
        return
        [
            .. ListenerModels.Select(x => new ListenOptions()
            {
                Ip = x.ListenerNetIpEndPoint.Address.ToString(),
                Port = x.ListenerNetIpEndPoint.Port,
            }),
        ];
    }
}

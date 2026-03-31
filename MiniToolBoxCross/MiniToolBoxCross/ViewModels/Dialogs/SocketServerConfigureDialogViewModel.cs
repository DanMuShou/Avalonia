using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Irihi.Avalonia.Shared.Contracts;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Common.Messages.SocketService;

namespace MiniToolBoxCross.ViewModels.Dialogs;

public partial class SocketServerConfigureDialogViewModel : ViewModelBase, IDialogContext
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private SocketConfigureType _serverType;

    [ObservableProperty]
    private IPAddress _serverIp;

    [ObservableProperty]
    private int _serverPort;

    [ObservableProperty]
    private IPAddress _localIp;

    [ObservableProperty]
    private int _localPort;

    public ObservableCollection<IPAddress> IpAddressList { get; set; }
    public event EventHandler<object?>? RequestClose;

    public SocketServerConfigureDialogViewModel(SocketConfigureType serverConfigureType)
    {
        ServerType = serverConfigureType;
        Title = serverConfigureType switch
        {
            SocketConfigureType.PortForwarding => "端口转发",
            SocketConfigureType.Communication => "通信",
            _ => throw new ArgumentOutOfRangeException(
                nameof(serverConfigureType),
                serverConfigureType,
                null
            ),
        };
        var ipAddressList = SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6);
        IpAddressList = new ObservableCollection<IPAddress>(ipAddressList);
        ServerIp = ipAddressList.FirstOrDefault() ?? IPAddress.None;
        ServerPort = SocketHelper.CheckPort(ServerPort) ?? 0;
        LocalIp = IPAddress.Loopback;
        LocalPort = SocketHelper.CheckPort(LocalPort) ?? 0;
    }

    [RelayCommand]
    public void Close() => RequestClose?.Invoke(this, null);

    [RelayCommand]
    private void Create()
    {
        WeakReferenceMessenger.Default.Send(
            new ValueChangedMessage<SocketServiceConfigureMessage>(
                new SocketServiceConfigureMessage("1", ServerIp, ServerPort, LocalIp, LocalPort)
            )
        );
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);
}

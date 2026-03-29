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
using MiniToolBoxCross.ViewModels;
using SuperSocket.Server.Abstractions;

namespace MiniToolBoxCross.Models.Entities;

public partial class SocketServerModel : ModelBase
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private IPAddress _ipAddress;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    // [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    // [NotifyCanExecuteChangedFor(nameof(RestartServerCommand))]
    private bool _isRunning;

    [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    // [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    // [NotifyCanExecuteChangedFor(nameof(RestartServerCommand))]
    private bool _isBusy;

    private bool CanStartServer() => !IsRunning && !IsBusy;

    private IServer? _server;

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
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketClientViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _serverIp = string.Empty;

    [ObservableProperty]
    private int _serverPort;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private ForwardModel? _selectedForward;

    public ObservableCollection<ForwardModel> ForwardModels { get; }
    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IClientForwardService _clientForwardService;

    public SocketClientViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService,
        IDialogService dialogService,
        IClientForwardService clientForwardService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _clientForwardService = clientForwardService;

        ForwardModels = [];
        ServerPort = 10295;
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (IsConnected || IsBusy)
            return;

        if (!IPAddress.TryParse(ServerIp, out var ip))
        {
            _notificationService.ShowError("连接失败", "服务器地址格式错误");
            return;
        }

        IsBusy = true;
        if (!_clientForwardService.IsConfigured)
            await _clientForwardService.ConfigAsync(new IPEndPoint(ip, ServerPort));
        var result = await _clientForwardService.ConnectAsync();
        if (result)
        {
            IsConnected = true;
            _notificationService.ShowSuccess("连接成功", "已成功连接到服务器");
        }
        else
            _notificationService.ShowError("连接失败", "无法连接到服务器");
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        if (!IsConnected || IsBusy)
            return;

        IsBusy = true;
        var result = await _clientForwardService.DisconnectAsync();
        if (result)
        {
            IsConnected = false;
            _notificationService.ShowSuccess("停止成功", "已成功断开与服务器的连接");
        }
        else
            _notificationService.ShowError("停止失败", "无法断开与服务器的连接");

        IsBusy = false;
    }

    [RelayCommand]
    private async Task Reconnect()
    {
        if (!IsConnected || IsBusy)
            return;

        IsBusy = true;
        var result = await _clientForwardService.ReconnectAsync();
        if (result)
        {
            IsConnected = true;
            _notificationService.ShowSuccess("重新连接成功", "已成功重新连接到服务器");
        }
        else
        {
            IsConnected = false;
            _notificationService.ShowError("重新连接失败", "无法重新连接到服务器");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private void Ping()
    {
        _notificationService.ShowSuccess("Ping", "Pong");
    }

    [RelayCommand]
    private async Task RemoveSelectedForwardModel()
    {
        if (SelectedForward is not null && ForwardModels.Contains(SelectedForward))
        {
            var result = await _clientForwardService.RemoveForwardServerAsync(SelectedForward.Id);
            if (result)
                ForwardModels.Remove(SelectedForward);
        }
    }

    [RelayCommand]
    private async Task OpenForwardConfigureDialog()
    {
        var model = await _dialogService.ShowCustomModalAsync<
            ForwardConfigureDialog,
            ForwardConfigureDialogViewModel,
            ForwardModel
        >(new ForwardConfigureDialogViewModel());
        if (model is not null)
        {
            var result = await _clientForwardService.AddForwardServerAsync(
                model.IpEndPoint,
                model.ForwardTargetType
            );
            if (result is not null)
            {
                model.Id = result.Id;
                model.IpEndPoint = new IPEndPoint(IPAddress.Parse(result.Address), result.Port);
                ForwardModels.Add(model);
            }
        }
    }

    [RelayCommand]
    private async Task Paste()
    {
        var text = await _crossSystemFunc.GetClipboardTextAsync();
        if (IPAddress.TryParse(text, out var ip))
            ServerIp = ip.ToString();
    }
}

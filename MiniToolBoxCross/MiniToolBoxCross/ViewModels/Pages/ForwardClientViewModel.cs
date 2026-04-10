using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class ForwardClientViewModel : ViewModelBase
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
    private readonly IForwardClientService _forwardClientService;

    public ForwardClientViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService,
        IDialogService dialogService,
        IForwardClientService forwardClientService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _forwardClientService = forwardClientService;

        ForwardModels = [];
        ServerPort = 10295;

        _forwardClientService.OnErrorOccurred = () =>
        {
            IsBusy = false;
            IsConnected = false;
            _notificationService.ShowError("客机异常", "客机异常");
        };
    }

    [RelayCommand]
    private Task Connect()
    {
        if (IsConnected || IsBusy)
            return Task.CompletedTask;

        if (!IPAddress.TryParse(ServerIp, out var ip))
        {
            _notificationService.ShowError("连接失败", "服务器地址格式错误");
            return Task.CompletedTask;
        }

        IsBusy = true;

        if (!_forwardClientService.IsConfigured)
            _forwardClientService.Config(new IPEndPoint(ip, ServerPort));

        var isConnected = _forwardClientService.Connect();
        if (isConnected)
        {
            IsConnected = true;
            _notificationService.ShowSuccess("连接成功", "已成功连接到服务器");
        }
        else
            _notificationService.ShowError("连接失败", "无法连接到服务器");

        IsBusy = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Disconnect()
    {
        if (!IsConnected || IsBusy)
            return Task.CompletedTask;

        IsBusy = true;

        var result = _forwardClientService.Disconnect();
        if (result)
        {
            IsConnected = false;
            _notificationService.ShowSuccess("停止成功", "已成功断开与服务器的连接");
        }
        else
            _notificationService.ShowError("停止失败", "无法断开与服务器的连接");
        IsBusy = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Reconnect()
    {
        if (!IsConnected || IsBusy)
            return Task.CompletedTask;

        IsBusy = true;

        var result = _forwardClientService.Reconnect();
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
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Ping()
    {
        _notificationService.ShowSuccess("Ping", "Pong");
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
            var server = _forwardClientService.AddForwardServer(
                model.IPEndPoint,
                model.ForwardTargetType
            );
            if (server is not null)
            {
                model.Id = server.Id;
                ForwardModels.Add(model);
                _notificationService.ShowSuccess("添加成功", "已成功添加转发服务器");
            }
            else
                _notificationService.ShowError("添加失败", "无法添加转发服务器");
        }
    }

    [RelayCommand]
    private void RemoveSelectedForwardModel()
    {
        if (SelectedForward is null || !ForwardModels.Contains(SelectedForward))
            return;
        var result = _forwardClientService.RemoveForwardServer(SelectedForward.Id);
        if (result)
        {
            ForwardModels.Remove(SelectedForward);
            _notificationService.ShowSuccess("删除成功", "已成功删除转发服务器");
        }
        else
            _notificationService.ShowError("删除失败", "无法删除转发服务器");
    }

    [RelayCommand]
    private async Task Paste()
    {
        var text = await _crossSystemFunc.GetClipboardTextAsync();
        if (IPAddress.TryParse(text, out var ip))
            ServerIp = ip.ToString();
    }
}

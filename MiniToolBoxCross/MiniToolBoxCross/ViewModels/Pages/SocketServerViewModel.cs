using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Services;
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private IPAddress _serverIp;

    [ObservableProperty]
    private int _serverPort;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ForwardModel? _selectedForward;

    public ObservableCollection<IPAddress> IpAddressList { get; }
    public ObservableCollection<ForwardModel> ForwardModels { get; }
    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IHostForwardService _hostForwardService;

    public SocketServerViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService,
        IDialogService dialogService,
        IHostForwardService hostForwardService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _hostForwardService = hostForwardService;
        IpAddressList = new ObservableCollection<IPAddress>(SocketHelper.GetIpAddressList());
        ForwardModels = [];

        ServerIp =
            SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6).FirstOrDefault()
            ?? IPAddress.None;
        ServerPort = 10295;
    }

    [RelayCommand]
    private async Task Start()
    {
        if (IsRunning || IsBusy)
            return;

        IsBusy = true;
        if (!_hostForwardService.IsConfigured)
            await _hostForwardService.ConfigAsync(new IPEndPoint(ServerIp, ServerPort));
        var result = await _hostForwardService.StartAsync();
        if (result)
        {
            IsRunning = true;
            _notificationService.ShowSuccess("启动成功", "服务器已启动");
        }
        else
            _notificationService.ShowError("启动失败", "服务器启动失败");
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Stop()
    {
        if (!IsRunning || IsBusy)
            return;

        IsBusy = true;
        var result = await _hostForwardService.StopAsync();
        IsRunning = result;
        if (result)
        {
            IsRunning = false;
            _notificationService.ShowSuccess("停止成功", "服务器已停止");
        }
        else
            _notificationService.ShowError("停止失败", "服务器停止失败");
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Restart()
    {
        if (!IsRunning || IsBusy)
            return;

        IsBusy = true;
        var result = await _hostForwardService.RestartAsync();
        if (result)
        {
            IsRunning = true;
            _notificationService.ShowSuccess("重启成功", "服务器已重启");
        }
        else
        {
            IsRunning = false;
            _notificationService.ShowError("重启失败", "服务器重启失败");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private void RemoveSelectedForwardModel()
    {
        if (SelectedForward is null || !ForwardModels.Contains(SelectedForward))
            return;
        var result = _hostForwardService.RemoveForwardClientAsync(SelectedForward.Id);
        if (result)
            ForwardModels.Remove(SelectedForward);
    }

    [RelayCommand]
    private async Task OpenForwardConfigureDialog()
    {
        var model = await _dialogService.ShowCustomModalAsync<
            ForwardConfigureDialog,
            ForwardConfigureDialogViewModel,
            ForwardModel
        >(new ForwardConfigureDialogViewModel());
        if (model is null)
            return;
        var result = _hostForwardService.AddForwardClientAsync(
            model.IpEndPoint,
            model.ForwardTargetType
        );
        if (result == null)
            return;
        model.Id = result.Id;
        model.IpEndPoint = new IPEndPoint(IPAddress.Parse(result.Address), result.Port);
        ForwardModels.Add(model);
    }

    [RelayCommand]
    private async Task CopyInfo()
    {
        await _crossSystemFunc.SetClipboardText(ServerIp.ToString());
    }
}

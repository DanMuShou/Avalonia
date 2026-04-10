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
using MiniToolBoxCross.ViewModels.Dialogs;
using MiniToolBoxCross.Views.Dialogs;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class ForwardHostViewModel : ViewModelBase
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

    private readonly IForwardHostService _forwardHostService;

    public ForwardHostViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService,
        IDialogService dialogService,
        IForwardHostService forwardHostService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _forwardHostService = forwardHostService;
        IpAddressList = new ObservableCollection<IPAddress>(SocketHelper.GetIpAddressList());
        ForwardModels = [];
        ServerIp =
            SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6).FirstOrDefault()
            ?? IPAddress.None;
        ServerPort = 10295;

        forwardHostService.OnErrorOccurred = () =>
        {
            IsRunning = false;
            IsBusy = false;
            _notificationService.ShowError("主机异常", "主机异常");
        };
    }

    [RelayCommand]
    private Task Start()
    {
        if (IsRunning || IsBusy)
            return Task.CompletedTask;

        IsBusy = true;

        if (!_forwardHostService.IsConfigured)
            _forwardHostService.Config(new IPEndPoint(ServerIp, ServerPort));

        var isStarted = _forwardHostService.Start();
        if (isStarted)
        {
            IsRunning = true;
            _notificationService.ShowSuccess("启动成功", "服务器已启动");
        }
        else
            _notificationService.ShowError("启动失败", "服务器启动失败");

        IsBusy = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Stop()
    {
        if (!IsRunning || IsBusy)
            return Task.CompletedTask;

        IsBusy = true;

        var result = _forwardHostService.Stop();
        if (result)
        {
            IsRunning = false;
            _notificationService.ShowSuccess("停止成功", "服务器已停止");
        }
        else
            _notificationService.ShowError("停止失败", "服务器停止失败");

        IsBusy = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Restart()
    {
        if (!IsRunning || IsBusy)
            return Task.CompletedTask;

        IsBusy = true;
        var result = _forwardHostService.Restart();
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
        return Task.CompletedTask;
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
        var client = _forwardHostService.AddForwardClient(
            model.IPEndPoint,
            model.ForwardTargetType
        );
        if (client is not null)
        {
            model.Id = client.Id;
            ForwardModels.Add(model);
            _notificationService.ShowSuccess("添加成功", "已成功添加转发客户端");
        }
        else
            _notificationService.ShowError("添加失败", "无法添加转发客户端");
    }

    [RelayCommand]
    private void RemoveSelectedForwardModel()
    {
        if (SelectedForward is null || !ForwardModels.Contains(SelectedForward))
            return;
        var result = _forwardHostService.RemoveForwardClient(SelectedForward.Id);
        if (result)
        {
            ForwardModels.Remove(SelectedForward);
            _notificationService.ShowSuccess("删除成功", "已成功删除转发客户端");
        }
        else
            _notificationService.ShowError("删除失败", "无法删除转发客户端");
    }

    [RelayCommand]
    private async Task CopyInfo()
    {
        await _crossSystemFunc.SetClipboardText(ServerIp.ToString());
    }
}

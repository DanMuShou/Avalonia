using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Entities;

namespace MiniToolBoxCross.ViewModels.Dialogs;

public partial class ForwardConfigureDialogViewModel : DialogBase, IDialogContext
{
    public event EventHandler<object?>? RequestClose;

    [ObservableProperty]
    private string _forwardName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "转发IP不能为空")]
    private IPAddress _forwardIp;

    [ObservableProperty]
    [Range(1024, 65535, ErrorMessage = "转发端口格式错误")]
    private int _forwardPort;

    [ObservableProperty]
    private ForwardTargetType _forwardTargetType = ForwardTargetType.Udp01;

    [ObservableProperty]
    private bool _isTcp;

    public ObservableCollection<IPAddress> IpAddressList { get; }

    public ForwardConfigureDialogViewModel()
    {
        IpAddressList = new ObservableCollection<IPAddress>(SocketHelper.GetIpAddressList(true));
        ForwardIp = IpAddressList.Contains(IPAddress.Loopback)
            ? IPAddress.Loopback
            : IpAddressList[0];
        ForwardPort = SocketHelper.GetRandomAvailablePort() ?? 0;
    }

    [RelayCommand]
    private void Create()
    {
        if (!IsValidation())
            return;

        RequestClose?.Invoke(
            this,
            new ForwardModel(ForwardName, ForwardTargetType, new IPEndPoint(ForwardIp, ForwardPort))
        );
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, null);

    public void Close() => RequestClose?.Invoke(this, null);
}

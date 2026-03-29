using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Global;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Entities;
using MiniToolBoxCross.Models.Repositories.Global;
using SuperSocket.Client;
using SuperSocket.ProtoBase;

namespace MiniToolBoxCross.ViewModels.Pages;

public partial class SocketClientViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private int _port = 19285;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(PingCommand))]
    private bool _isBusy;

    private readonly CrossSystemFunc _crossSystemFunc;
    private readonly INotificationService _notificationService;
    private readonly IEasyClient<StringPackageInfo> _client;

    public SocketClientViewModel(
        CrossSystemFunc crossSystemFunc,
        INotificationService notificationService
    )
    {
        _crossSystemFunc = crossSystemFunc;
        _notificationService = notificationService;

        _client = new EasyClient<StringPackageInfo>(new CommandLinePipelineFilter());
        _client.PackageHandler += ClientOnPackageHandler;
        _client.Closed += ClientOnClosed;
    }

    private bool CanConnect() => !IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task Connect()
    {
        if (IsConnected || IsBusy)
            return;

        if (!IPAddress.TryParse(IpAddress, out var ipAddress))
        {
            _notificationService.ShowWarning("验证失败", "请输入有效的 IP 地址");
            return;
        }
        if (Port is <= 0 or > 65535)
        {
            _notificationService.ShowWarning("验证失败", "请输入有效的端口号（1-65535）");
            return;
        }

        var endPoint = new IPEndPoint(ipAddress, Port);

        IsBusy = true;
        try
        {
            var result = await _client.ConnectAsync(endPoint);
            IsConnected = result;

            if (result)
                _notificationService.ShowSuccess("连接成功", $"已成功连接到 {endPoint}");
            else
                _notificationService.ShowWarning(
                    "连接失败",
                    $"无法连接到 {endPoint}，请检查网络或服务器状态"
                );

            if (result)
            {
                _client.StartReceive();

                await Task.Delay(100);

                var loginInfo = new LoginRequest
                {
                    Name = "test",
                    Password = "test",
                    InvitationCode = Guid.NewGuid(),
                };

                await _client.SendAsync(
                    CommandLinePackageEncoder.Encode(
                        nameof(SocketCommandType.Login),
                        JsonSerializer.Serialize(loginInfo)
                    )
                );

                // 显示连接成功通知
                _notificationService.ShowSuccess("连接成功", $"已成功连接到 {endPoint}");
            }
            else
            {
                // 显示连接失败通知
                _notificationService.ShowError(
                    "连接失败",
                    $"无法连接到 {endPoint}，请检查网络或服务器状态"
                );
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("连接异常", $"连接过程中发生错误：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanPing() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanPing))]
    private async Task Ping()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            _notificationService.ShowWarning("验证失败", "请输入有效的 IPv6 地址");
            return;
        }

        try
        {
            using var ping = new Ping();
            var pingOptions = new PingOptions { Ttl = 128, DontFragment = false };
            var buffer = new byte[32];
            const int timeout = 5000; // 5 秒超时

            var reply = await ping.SendPingAsync(IpAddress, timeout, buffer, pingOptions);

            if (reply.Status == IPStatus.Success)
            {
                _notificationService.ShowSuccess("Ping 成功", $"往返时间：{reply.RoundtripTime}ms");
            }
            else
            {
                _notificationService.ShowWarning("Ping 失败", $"目标主机不可达：{reply.Status}");
            }
        }
        catch (PingException ex)
        {
            _notificationService.ShowError("网络错误", $"Ping 过程中发生网络错误：{ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _notificationService.ShowError("参数错误", $"IP 地址格式不正确：{ex.Message}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("操作失败", $"Ping 过程中发生未知错误：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PasteIp()
    {
        var text = await _crossSystemFunc.GetClipboardText();
        if (IPEndPoint.TryParse(text, out var ipEndPoint))
        {
            IpAddress = ipEndPoint.Address.ToString();
            Port = ipEndPoint.Port;
        }
    }

    private bool CanDisconnect() => IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task Disconnect()
    {
        if (!IsConnected || IsBusy)
            return;
        IsBusy = true;

        try
        {
            await _client.CloseAsync();
            IsConnected = false;
            // 弹窗会自动记录日志
            _notificationService.ShowInformation("断开连接", "已成功断开与服务器的连接");
        }
        catch (Exception ex)
        {
            // 弹窗会自动记录日志
            _notificationService.ShowError("断开失败", $"断开连接时发生错误：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async ValueTask ClientOnPackageHandler(
        EasyClient<StringPackageInfo> sender,
        StringPackageInfo package
    )
    {
        await Task.Delay(0);
    }

    private void ClientOnClosed(object? sender, EventArgs e)
    {
        IsConnected = false;
        _notificationService.ShowInformation("连接断开", "与服务器的连接已断开");
    }
}

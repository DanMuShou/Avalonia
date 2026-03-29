using System;
using System.Threading.Tasks;
using MiniToolBoxCross.Models.Entities;
using SuperSocket.Connection;
using SuperSocket.Server;

namespace MiniToolBoxCross.Services;

public class SocketSession : AppSession
{
    /// <summary>
    /// 客户端是否已通过认证
    /// </summary>
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// 登录信息
    /// </summary>
    public LoginRequest? LoginInfo { get; private set; }

    /// <summary>
    /// 获取 Socket 服务实例（由 SuperSocket 框架自动注入）
    /// </summary>
    private ISocketService Service =>
        Server as ISocketService ?? throw new Exception("SocketService not found.");

    protected override async ValueTask OnSessionConnectedAsync()
    {
        await base.OnSessionConnectedAsync();
    }

    protected override ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        return base.OnSessionClosedAsync(e);
    }

    /// <summary>
    /// 处理登录认证
    /// </summary>
    /// <param name="loginInfo">登录信息</param>
    /// <returns>是否认证成功</returns>
    public async Task AuthenticateAsync(LoginRequest? loginInfo)
    {
        if (
            loginInfo is null
            || string.IsNullOrWhiteSpace(loginInfo.Name)
            || string.IsNullOrWhiteSpace(loginInfo.Password)
        )
        {
            IsAuthenticated = false;
            LoginInfo = null;
            await CloseAsync();
            return;
        }

        IsAuthenticated = true;
        LoginInfo = loginInfo;
        await Service.RegisterAuthAsync(this);
    }
}

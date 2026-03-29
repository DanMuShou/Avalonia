using System;

namespace MiniToolBoxCross.Models.Entities;

/// <summary>
/// 登录认证信息
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邀请码 (Guid)
    /// </summary>
    public Guid InvitationCode { get; set; }
}

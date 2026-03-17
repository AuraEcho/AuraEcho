using System.ComponentModel;

namespace AuraEcho.Core.Models.Api;

public enum ResultStatus
{
    [Description("成功")]
    Success = 0,

    [Description("邮箱验证码错误")]
    EmailCodeError = 1,

    [Description("账号或密码错误")]
    PasswordError = 2,
}

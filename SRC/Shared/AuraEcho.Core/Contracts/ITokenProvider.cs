using AuraEcho.Cloud.V1.Hub;

namespace AuraEcho.Core.Contracts;

/// <summary>
/// Token 状态管理抽象。继承 <see cref="IHubTokenProvider"/>，
/// 在其基础上增加 Token 的写入、清除与刷新能力。
/// </summary>
public interface ITokenProvider : IHubTokenProvider
{
    /// <summary>
    /// 是否已登录（存在有效的 Token）。
    /// </summary>
    bool IsSignedIn { get; }

    /// <summary>
    /// 获取当前的 RefreshToken。
    /// </summary>
    string? RefreshToken { get; }

    /// <summary>
    /// 获取当前 Access Token 的 JTI（JWT ID），用于单设备登录校验。
    /// </summary>
    string? Jti { get; }

    /// <summary>
    /// 写入新的 Token 并持久化 RefreshToken。
    /// </summary>
    void SetToken(string accessToken, string refreshToken, DateTimeOffset expiresAt, string jti);

    /// <summary>
    /// 清除 Token 及持久化的 RefreshToken。
    /// </summary>
    void ClearToken();

    /// <summary>
    /// 尝试使用 RefreshToken 刷新 AccessToken。
    /// 成功时更新内部状态并返回 true；失败时清除 Token 并返回 false。
    /// </summary>
    Task<bool> TryRefreshTokenAsync();
}

using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

/// <summary>
/// 客户端会话抽象。负责协调登录/登出流程、用户资料管理与 Hub 连接生命周期。
/// Token 状态由 <see cref="ITokenProvider"/> 集中管理。
/// </summary>
public interface IClientSession
{
    bool IsSignedIn { get; }
    UserProfile? CurrentUser { get; }
    void SignIn(AuthResponse authResponse);
    void UpdateUserProfile(UserProfile userProfile);
    void SignOut();
}

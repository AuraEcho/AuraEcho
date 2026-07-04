using AuraEcho.ClientApi.V1.Auth;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IClientSession
{
    bool IsSignedIn { get; }
    AppToken? AppToken { get; }

    UserProfile? CurrentUser { get; }
    Task<bool> TryRefreshTokenAsync();
    void SignIn(AuthResponse appToken);
    void UpdateUserProfile(UserProfile userProfile);
    void SignOut();
}

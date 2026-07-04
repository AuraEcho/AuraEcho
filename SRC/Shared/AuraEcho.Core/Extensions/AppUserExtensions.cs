using AuraEcho.ClientApi.V1.Auth;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Extensions;

public static class AppUserExtensions
{
    public static AppUserDto ToAppUser(this UserProfile @this)
        => new()
        {
            Email = @this.Email,
            UserId = @this.Id,
            UserName = @this.UserName,
            AvatarFileId = @this.AvatarFileId,
            AvatarFileUrl = @this.AvatarFileUrl
        };

    public static UserProfile ToUserProfile(this AppUserDto @this)
        => new()
        {
            UserName = @this.UserName,
            Email = @this.Email,
            Id = @this.UserId,
            AvatarFileId = @this.AvatarFileId,
            AvatarFileUrl = @this.AvatarFileUrl
        };
}

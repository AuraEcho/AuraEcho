using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Extensions;

public static class UserPluginExtensions
{
    public static UserPlugin ToUserPlugin(this UserPluginModel @this)
        => new()
        {
            Id = @this.Id,
            LocalPluginId = @this.LocalPlugin.Id,
            UserId = @this.UserId,
            Status = @this.Status
        };

    public static UserPluginModel ToUserPluginModel(this UserPlugin @this)
        => new()
        {
            Id = @this.Id,
            Status = @this.Status,
            UserId = @this.UserId,
            LocalPlugin = @this.LocalPlugin?.ToLocalPluginModel()
        };
}

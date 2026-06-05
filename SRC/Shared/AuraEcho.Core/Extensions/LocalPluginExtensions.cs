using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Extensions;

public static class LocalPluginExtensions
{
    public static InstalledPlugin ToLocalPlugin(this InstalledPluginModel @this)
        => new()
        {
            PluginId = @this.Id,
            IsSetup = @this.IsSetup,
            Id = @this.Id,
            InstallPath = @this.InstallPath,
            PluginType = @this.PluginType,
            InstaledAt = @this.InstaledAt,
            Version = @this.Version,
        };

    public static InstalledPluginModel ToLocalPluginModel(this InstalledPlugin @this)
        => new()
        {
            PluginId = @this.Id,
            IsSetup = @this.IsSetup,
            Id = @this.Id,
            InstallPath = @this.InstallPath,
            PluginType = @this.PluginType,
            InstaledAt = @this.InstaledAt,
            Version = @this.Version
        };
}

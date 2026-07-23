using AuraEcho.Domain;
using AuraEcho.Persistence.Entities;

namespace AuraEcho.Persistence.Extensions;

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

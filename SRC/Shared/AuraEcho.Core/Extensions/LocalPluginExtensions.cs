using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Extensions;

public static class LocalPluginExtensions
{
    public static LocalPlugin ToLocalPlugin(this LocalPluginModel @this)
        => new()
        {
            PluginFolder = @this.PluginFolder,
            Id = @this.Id,
            Manifest = @this.Manifest,
            IsSetup = @this.IsSetup
        };

    public static LocalPluginModel ToLocalPluginModel(this LocalPlugin @this)
        => new()
        {
            PluginFolder = @this.PluginFolder,
            Id = @this.Id,
            Manifest = @this.Manifest,
            IsSetup = @this.IsSetup
        };
}

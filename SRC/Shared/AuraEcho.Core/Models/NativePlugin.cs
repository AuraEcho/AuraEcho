using System.Windows;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models;

public class NativePlugin : AppPlugin
{
    public IPlugin PluginContext { get; set; }
    public NativePlugin(PluginManifest manifest) : base(manifest)
    {
    }

    public override ResourceDictionary? GetThemeResource(AppTheme theme)
        => PluginContext.GetThemeResource(theme);

    public override AppSettingsItem? GetSettings()
        => PluginContext.GetSettings();
}

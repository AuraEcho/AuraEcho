using AuraEcho.PluginContracts.Models;
using System.Windows;

namespace AuraEcho.Core.Models;

public class RemoteWebPlugin : AppPlugin
{
    public string RemoteUrl
    {
        get;
        set => SetProperty(ref field, value);
    }
    public RemoteWebPlugin(RemoteWebPluginManifest manifest) : base(manifest)
    {
        RemoteUrl = manifest.RemoteUrl;
    }

    public override ResourceDictionary? GetThemeResource(AppTheme theme) => null;

    public override PluginSettingsItem? GetSettings() => null;
}

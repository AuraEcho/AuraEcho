using AuraEcho.PluginContracts.Models;
using System.Windows;

namespace AuraEcho.Core.Models;

public class LocalWebPlugin : AppPlugin
{
    public string EntryFileName
    {
        get;
        set => SetProperty(ref field, value);
    }
    
    public LocalWebPlugin(LocalWebPluginManifest manifest) : base(manifest)
    {
        EntryFileName = manifest.EntryFileName;
    }

    public override AppSettingsItem? GetSettings() => null;

    public override ResourceDictionary? GetThemeResource(AppTheme theme) => null;
}

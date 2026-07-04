using System.Windows;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models;

public class NativePlugin : AppPlugin
{
    public string EntryFileName
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IPlugin PluginContext { get; set; }
    
    public NativePlugin(NativePluginManifest manifest) : base(manifest)
    {
        EntryFileName = manifest.EntryFileName;
    }

    public override ResourceDictionary? GetThemeResource(AppTheme theme)
        => PluginContext.GetThemeResource(theme);
}

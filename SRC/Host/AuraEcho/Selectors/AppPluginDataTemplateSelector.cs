using System.Windows;
using System.Windows.Controls;
using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Core.Models;

namespace AuraEcho.Selectors;

public class AppPluginDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NativePluginTemplate { get; set; }
    public DataTemplate? StandalonePluginTemplate { get; set; }
    public DataTemplate? LocalWebPluginTemplate { get; set; }
    public DataTemplate? RemoteWebPluginTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not AppPlugin plugin) return null;

        return plugin.PluginType switch
        {
            PluginType.Native => NativePluginTemplate,
            PluginType.Standalone => StandalonePluginTemplate,
            PluginType.LocalWeb => LocalWebPluginTemplate,
            PluginType.RemoteWeb => RemoteWebPluginTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}

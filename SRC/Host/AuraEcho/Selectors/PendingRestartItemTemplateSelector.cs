using System.Windows;
using System.Windows.Controls;
using AuraEcho.Models;

namespace AuraEcho.Selectors;

public class PendingRestartItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? AppUpdateTemplate { get; set; }
    public DataTemplate? PluginUpdateTemplate { get; set; }
    public DataTemplate? PluginUninstallTemplate { get; set; }
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
    {
        AppUpdatePendingRestart => AppUpdateTemplate,
        PluginUpdatePendingRestart => PluginUpdateTemplate,
        PluginUninstallPendingRestart => PluginUninstallTemplate,
        _ => base.SelectTemplate(item, container)
    };
}

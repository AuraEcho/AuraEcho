using System;
using System.Windows;
using System.Windows.Controls;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Selectors;

public class AppSettingsItemDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate HostSettingsItemTemplate { get; set; }
    public DataTemplate PluginSettingsItemTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is not AppSettingsItem asi) return null;

        return asi switch
        {
            HostSettingsItem => HostSettingsItemTemplate,
            PluginSettingsItem => PluginSettingsItemTemplate,
            _ => throw new Exception("不支持的设置项")
        };
    }
}

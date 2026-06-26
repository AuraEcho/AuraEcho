using System;
using System.Windows;
using System.Windows.Controls;
using AuraEcho.Enums;

namespace AuraEcho.Selectors;

public class MarketPluginStateTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NotInstalledTemplate { get; set; }
    public DataTemplate? AcquiringTemplate { get; set; }
    public DataTemplate? DownloadingTemplate { get; set; }
    public DataTemplate? InstallingTemplate { get; set; }
    public DataTemplate? InstalledTemplate { get; set; }
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is MarketPluginInstallStatus ts)
        {
            return ts switch
            {
                MarketPluginInstallStatus.None => NotInstalledTemplate,
                MarketPluginInstallStatus.Waiting => throw new NotImplementedException(),
                MarketPluginInstallStatus.Acquiring => AcquiringTemplate,
                MarketPluginInstallStatus.Downloading => DownloadingTemplate,
                MarketPluginInstallStatus.Installing => InstallingTemplate,
                MarketPluginInstallStatus.Completed => InstalledTemplate,
                MarketPluginInstallStatus.Canceled => NotInstalledTemplate,
                MarketPluginInstallStatus.Failed => NotInstalledTemplate,
                _ => base.SelectTemplate(item, container),
            };
        }
        return base.SelectTemplate(item, container);
    }
}

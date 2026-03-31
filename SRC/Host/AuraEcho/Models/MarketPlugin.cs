using AuraEcho.Core.Models;
using AuraEcho.Enums;
using AuraEcho.Services;
using Prism.Mvvm;

namespace AuraEcho.Models;

public class MarketPlugin : BindableBase
{
    public RemotePlugin PluginInfo { get; set; }
    public MarketPluginStatus Status { get; set; }
    public MarketPluginInstallTask InstallContext { get; set; }
}

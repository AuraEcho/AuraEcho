using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuraEcho.Constants;
using AuraEcho.Core.Models;
using AuraEcho.Domain;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Telemetry;
using Prism.Regions;

namespace AuraEcho.Services;

public class PluginLaunchService : IPluginLaunchService
{
    private readonly INavigationService _navigationService;
    private readonly IPluginManager _pluginManager;
    private readonly ITelemetryService _telemetry;

    public PluginLaunchService(
        INavigationService navigationService,
        IPluginManager pluginManager,
        ITelemetryService telemetry)
    {
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _telemetry = telemetry;
    }

    public bool Launch(Guid pluginId)
    {
        AppPlugin? targetPlugin = _pluginManager.Plugins.FirstOrDefault(p => p.PluginId == pluginId);
        if (targetPlugin is null) return false;

        Launch(targetPlugin);
        return true;
    }

    public void Launch(AppPlugin plugin)
    {
        if (plugin is null) return;

        _telemetry?.TrackEvent("Plugin.Opened", new Dictionary<string, string>
        {
            ["pluginId"] = plugin.PluginId.ToString(),
            ["pluginType"] = plugin.PluginType.ToString()
        });

        switch (plugin.PluginType)
        {
            case PluginType.Native:
                if (plugin is not NativePlugin nativePlugin) return;
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    nativePlugin.PluginContext.EntryViewName,
                    new NavigationParameters
                    {
                        {  "PluginId", nativePlugin.PluginId  },
                    });
                break;
            case PluginType.Standalone:
                (plugin as StandalonePlugin).Open();
                break;
            case PluginType.LocalWeb:
                var localWebPlugin = plugin as LocalWebPlugin;
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    ViewNames.WebContainer,
                    new NavigationParameters
                    {
                        {  "SourceUri", Path.Combine(localWebPlugin.WorkingDirectory, localWebPlugin.EntryFileName)  },
                    });
                break;
            case PluginType.RemoteWeb:
                var remoteWebPlugin = plugin as RemoteWebPlugin;
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    ViewNames.WebContainer,
                    new NavigationParameters
                    {
                        {  "SourceUri", remoteWebPlugin.RemoteUrl },
                    });
                break;
            default: throw new NotImplementedException("TODO: 不同类型插件的打开方式不同，待实现");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Cloud.V1;
using AuraEcho.Constants;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Domain;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Telemetry;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class MarketplacePluginDetailsViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly ApiClient _apiClient;
    private readonly ITransferManager _transferManager;
    private readonly INavigationService _navigationService;
    private readonly IPluginManager _pluginManager;
    private readonly IPluginLaunchService _pluginLaunchService;
    private readonly ITelemetryService _telemetry;

    public MarketPlugin MarketPlugin
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand InstallCommand { get; }
    private void Install()
    {
        _telemetry.TrackEvent("Marketplace.InstallClicked", new Dictionary<string, string>
        {
            ["pluginId"] = MarketPlugin.PluginInfo.Id.ToString()
        });
        _transferManager.AddTask(MarketPlugin.InstallContext);
    }

    public DelegateCommand OpenPluginCommand { get; }
    private void OpenPlugin()
    {
        AppPlugin? targetPlugin =
            _pluginManager.Plugins.FirstOrDefault(p => p.PluginId == MarketPlugin.PluginInfo.Id)
            ?? throw new Exception();

        _pluginLaunchService.Launch(targetPlugin);
    }

    public DelegateCommand<PluginScreenshot> NavigationToViewScreenshotCommand { get; }
    private void NavigationToViewScreenshot(PluginScreenshot ss)
    {
        _navigationService.RequestNavigate(
            HostRegionNames.DialogRegion,
            ViewNames.ImageViewer,
            new NavigationParameters
            {
                { "ImageFilePath", ss.FileUrl }
            },
            canBack: false);
    }

    private async Task LoadPluginScreenshotsAsync()
    {
        var response = await _apiClient.Plugin.GetScreenshotsAsync(MarketPlugin.PluginInfo.Id);
        MarketPlugin.PluginInfo.Screenshots = response?.Screenshots?.Select(s => s.ToPluginScreenshot()).ToList();
    }

    private async Task LoadPluginDetails()
    {
        var response = await _apiClient.Plugin.GetLatestAsync(MarketPlugin.PluginInfo.Id);
        MarketPlugin.PluginInfo.LatestVersion = response?.ToPluginPackage();
    }

    public MarketplacePluginDetailsViewModel(
        ApiClient apiClient,
        INavigationService navigationService,
        IPluginManager pluginManager,
        IPluginLaunchService pluginLaunchService,
        ITransferManager transferManager,
        ITelemetryService telemetry)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _pluginLaunchService = pluginLaunchService;
        _transferManager = transferManager;
        _telemetry = telemetry;

        OpenPluginCommand = new DelegateCommand(OpenPlugin);
        InstallCommand = new DelegateCommand(Install);
        NavigationToViewScreenshotCommand = new DelegateCommand<PluginScreenshot>(NavigationToViewScreenshot);
    }

    public bool KeepAlive => false;

    public bool IsNavigationTarget(NavigationContext navigationContext)
        => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        MarketPlugin = navigationContext.Parameters["Plugin"] as MarketPlugin;
        _telemetry.TrackEvent("Marketplace.PluginViewed", new Dictionary<string, string>
        {
            ["pluginId"] = MarketPlugin?.PluginInfo.Id.ToString() ?? string.Empty
        });
        _ = LoadPluginScreenshotsAsync();
        _ = LoadPluginDetails();
    }
}

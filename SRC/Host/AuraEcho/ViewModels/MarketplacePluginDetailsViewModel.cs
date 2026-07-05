using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Plugin;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class MarketplacePluginDetailsViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly ApiClient _apiClient;
    private readonly ITransferManager _transferManager;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly INavigationService _navigationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IPluginManager _pluginManager;

    public MarketPlugin MarketPlugin
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand InstallCommand { get; }
    private void Install()
    {
        _transferManager.AddTask(MarketPlugin.InstallContext);
    }

    public DelegateCommand OpenPluginCommand { get; }
    private void OpenPlugin()
    {
        AppPlugin? targetPlugin =
            _pluginManager.Plugins.FirstOrDefault(p => p.PluginId == MarketPlugin.PluginInfo.Id)
            ?? throw new Exception();

        switch (targetPlugin.PluginType)
        {
            case PluginType.Native:
                if (targetPlugin is not NativePlugin nativePlugin) return;
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    nativePlugin.PluginContext.EntryViewName,
                    new NavigationParameters
                    {
                        {  "PluginId", nativePlugin.PluginId  },
                    });
                break;
            case PluginType.Standalone:
                (targetPlugin as StandalonePlugin).Open();
                break;
            case PluginType.LocalWeb:
                var localWebPlugin = targetPlugin as LocalWebPlugin;
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    ViewNames.WebContainer,
                    new NavigationParameters
                    {
                        {  "SourceUri", Path.Combine(localWebPlugin.WorkingDirectory, localWebPlugin.EntryFileName)  },
                    });
                break;
            case PluginType.RemoteWeb:
                var remoteWebPlugin = targetPlugin as RemoteWebPlugin;
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
        IEventAggregator eventAggregator,
        IPluginInstallService pluginInstallService,
        IPluginManager pluginManager,
        ITransferManager transferManager)
    {
        _apiClient = apiClient;
        _pluginInstallService = pluginInstallService;
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _eventAggregator = eventAggregator;
        _transferManager = transferManager;

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
        _ = LoadPluginScreenshotsAsync();
        _ = LoadPluginDetails();
    }
}

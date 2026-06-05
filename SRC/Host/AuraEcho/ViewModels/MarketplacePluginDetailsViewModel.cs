using System;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Constants;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class MarketplacePluginDetailsViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IRemotePluginRepository _pluginRepository;
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
                    nativePlugin.PluginContext.EntryViewName);
                break;
            default: throw new NotImplementedException("TODO: 不同类型插件的打开方式不同，待实现");
        }
    }

    public DelegateCommand<PluginScreenshot> NavigationToViewScreenshotCommand { get; }
    private void NavigationToViewScreenshot(PluginScreenshot ss)
    {
        _navigationService.RequestNavigate(
            HostRegionNames.ContentDialogRegion,
            ViewNames.ImageViewer,
            new NavigationParameters
            {
                { "ImageFilePath", ss.FileUrl }
            },
            canBack: false);
    }

    private async Task LoadPluginScreenshotsAsync()
    {
        var screenshots = await _pluginRepository.GetScreenshotsAsync(MarketPlugin.PluginInfo.Id);
        MarketPlugin.PluginInfo.Screenshots = screenshots;
    }

    private async Task LoadPluginDetails()
    {
        var result = await _pluginRepository.GetLatestAsync(MarketPlugin.PluginInfo.Id);
        MarketPlugin.PluginInfo.LatestVersion = result;
    }

    public MarketplacePluginDetailsViewModel(
        IRemotePluginRepository pluginRepository,
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IPluginInstallService pluginInstallService, 
        IPluginManager pluginManager,
        ITransferManager transferManager)
    {
        _pluginRepository = pluginRepository;
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

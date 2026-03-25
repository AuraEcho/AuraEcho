using System;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
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

    public PluginPackage LatestVersionInfo
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
        UserPluginModel? targetRegistry = 
            _pluginManager.Plugins.FirstOrDefault(p => p.LocalPlugin.Manifest.Id == MarketPlugin.PluginInfo.Id) 
            ?? throw new Exception();

        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            targetRegistry.LocalPlugin.Manifest.DefaultViewName);
    }

    private async Task LoadPluginDetails()
    {
        var result = await _pluginRepository.GetLatestAsync(MarketPlugin.PluginInfo.Id);
        if (result is null) return;

        LatestVersionInfo = result;
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
        _ = LoadPluginDetails();
    }
}

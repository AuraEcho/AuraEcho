using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class PluginsMarketplaceViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly IRemotePluginRepository _pluginRespository;
    private readonly INavigationService _navigationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IPluginManager _pluginManager;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly ITransferManager _transferManager;
    private readonly ILocalPluginRepository _localPluginRespository;
    private readonly IClientSession _clientSession;

    public ObservableCollection<MarketPlugin> Plugins
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand LoadPluginsCommand { get; }
    private async void LoadPlugins()
    {
        var result = await _pluginRespository.GetPluginsAsync();
        if (result is null) return;

        List<Guid> installedPluginIds = _pluginManager.Plugins.Select(p => p.LocalPlugin.Manifest.Id).ToList();
        List<PluginDownloadTask> inProcessTasks = [.. _transferManager.AllTasks.OfType<PluginDownloadTask>()];
        ObservableCollection<MarketPlugin> marketPlugins = result.Select(ToMarketPlugin).ToObservableCollection();

        Plugins = [.. marketPlugins];

        MarketPlugin ToMarketPlugin(AppPlugin plugin)
        {
            if (installedPluginIds.Contains(plugin.Id))
            {
                return new MarketPlugin
                {
                    PluginInfo = plugin,
                    InstallContext = PluginDownloadTask.CreateAsCompleted()
                };
            }

            PluginDownloadTask installContext =
                inProcessTasks.FirstOrDefault(t => t.Id == plugin.Id.ToString()) 
                ?? new PluginDownloadTask(
                    _pluginRespository,
                    _pluginInstallService,
                    _pluginManager,
                    _eventAggregator,
                    _localPluginRespository,
                    _clientSession,
                    plugin.Id,
                    plugin.DisplayName);

            return new MarketPlugin
            {
                PluginInfo = plugin,
                InstallContext = installContext
            };
        }
    }

    public DelegateCommand<MarketPlugin> InstallPluginCommand { get; }
    private void InstallPlugin(MarketPlugin plugin)
    {
        _transferManager.AddTask(plugin.InstallContext);
    }

    public DelegateCommand<MarketPlugin> OpenPluginCommand { get; }
    private void OpenPlugin(MarketPlugin plugin)
    {
        UserPluginModel? targetRegistry =
            _pluginManager.Plugins.FirstOrDefault(p => p.LocalPlugin.Manifest.Id == plugin.PluginInfo.Id)
            ?? throw new Exception();

        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            targetRegistry.LocalPlugin.Manifest.DefaultViewName);
    }

    public DelegateCommand<MarketPlugin> NavigationToPluginDetailsCommand { get; }
    private void NavigationToPluginDetails(MarketPlugin plugin)
    {
        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            ViewNames.MarketplacePluginDetails,
            new NavigationParameters
            {
                { "Plugin", plugin }
            });
    }

    public PluginsMarketplaceViewModel(
        IPluginManager pluginManager, 
        INavigationService navigationService, 
        IRemotePluginRepository pluginRespository,
        IPluginInstallService pluginInstallService,
        IEventAggregator eventAggregator,
        ITransferManager transferManager,
        ILocalPluginRepository localPluginRepository,
        IClientSession clientSession)
    {
        _clientSession = clientSession;
        _localPluginRespository = localPluginRepository;
        _transferManager = transferManager;
        _eventAggregator = eventAggregator;
        _pluginRespository = pluginRespository;
        _pluginInstallService = pluginInstallService;
        _navigationService = navigationService;
        _pluginManager = pluginManager;

        LoadPluginsCommand = new DelegateCommand(LoadPlugins);
        InstallPluginCommand = new DelegateCommand<MarketPlugin>(InstallPlugin);
        OpenPluginCommand = new DelegateCommand<MarketPlugin>(OpenPlugin);
        NavigationToPluginDetailsCommand = new DelegateCommand<MarketPlugin>(NavigationToPluginDetails);
        LoadPlugins();
    }
    public bool KeepAlive => false;
}

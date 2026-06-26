using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Enums;
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
    private readonly IStorageRepository _storageRepository;
    private readonly IAuraToastService _auraToastService;

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

        List<Guid> installedPluginIds = _pluginManager.Plugins.Select(p => p.PluginId).ToList();
        List<MarketPluginInstallTask> inProcessTasks = [.. _transferManager.AllTasks.OfType<MarketPluginInstallTask>()];
        ObservableCollection<MarketPlugin> marketPlugins = result.Select(ToMarketPlugin).ToObservableCollection();

        Plugins = [.. marketPlugins];

        MarketPlugin ToMarketPlugin(RemotePlugin plugin)
        {
            if (installedPluginIds.Contains(plugin.Id))
            {
                var mp = new MarketPlugin
                {
                    PluginInfo = plugin,
                    Status = MarketPluginStatus.Installed,
                };
                mp.InstallContext = MarketPluginInstallTask.CreateAsCompleted(mp);
                return mp;
            }

            var marketPlugin = new MarketPlugin
            {
                PluginInfo = plugin,
                Status = plugin.IsAcquired ? MarketPluginStatus.Acquired : MarketPluginStatus.None
            };
            marketPlugin.InstallContext =
                inProcessTasks.FirstOrDefault(t => t.Id == plugin.Id.ToString())
                ?? new MarketPluginInstallTask(
                    _pluginRespository,
                    _pluginInstallService,
                    _pluginManager,
                    _eventAggregator,
                    _localPluginRespository,
                    _clientSession,
                    _storageRepository,
                    _auraToastService,
                    marketPlugin);
            return marketPlugin;
        }
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
        IClientSession clientSession,
        IStorageRepository storageRepository,
        IAuraToastService auraToastService)
    {
        _clientSession = clientSession;
        _localPluginRespository = localPluginRepository;
        _transferManager = transferManager;
        _eventAggregator = eventAggregator;
        _storageRepository = storageRepository;
        _pluginRespository = pluginRespository;
        _pluginInstallService = pluginInstallService;
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _auraToastService = auraToastService;

        LoadPluginsCommand = new DelegateCommand(LoadPlugins);
        NavigationToPluginDetailsCommand = new DelegateCommand<MarketPlugin>(NavigationToPluginDetails);
        LoadPlugins();
    }
    public bool KeepAlive => false;
}

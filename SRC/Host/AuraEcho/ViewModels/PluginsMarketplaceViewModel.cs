using AuraEcho.Telemetry;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.EndPoints;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Persistence.Contracts;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AuraEcho.ViewModels;

public class PluginsMarketplaceViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly ApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IPluginManager _pluginManager;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly ITransferManager _transferManager;
    private readonly ILocalPluginRepository _localPluginRespository;
    private readonly IClientSession _clientSession;
    private readonly IAuraToastService _auraToastService;
    private readonly ITelemetryService _telemetry;

    public ObservableCollection<MarketPlugin> Plugins
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand LoadPluginsCommand { get; }
    private async void LoadPlugins()
    {
        var response = await _apiClient.Plugin.GetPluginsAsync();
        if (response?.Data is null) return;
        var result = response.Data.Select(p => p.ToRemotePlugin()).ToList();

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
                    _apiClient,
                    _pluginInstallService,
                    _pluginManager,
                    _eventAggregator,
                    _localPluginRespository,
                    _clientSession,
                    _auraToastService,
                    _telemetry,
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
        ApiClient apiClient,
        IPluginInstallService pluginInstallService,
        IEventAggregator eventAggregator,
        ITransferManager transferManager,
        ILocalPluginRepository localPluginRepository,
        IClientSession clientSession,
        IAuraToastService auraToastService,
        ITelemetryService telemetry)
    {
        _clientSession = clientSession;
        _localPluginRespository = localPluginRepository;
        _transferManager = transferManager;
        _eventAggregator = eventAggregator;
        _apiClient = apiClient;
        _pluginInstallService = pluginInstallService;
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _auraToastService = auraToastService;
        _telemetry = telemetry;

        LoadPluginsCommand = new DelegateCommand(LoadPlugins);
        NavigationToPluginDetailsCommand = new DelegateCommand<MarketPlugin>(NavigationToPluginDetails);
        LoadPlugins();
    }
    public bool KeepAlive => false;
}

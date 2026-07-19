using AuraEcho.Cloud.V1.Models.Plugin;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Strings;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
namespace AuraEcho.ViewModels;

public class HomepageViewModel : BindableBase, IRegionMemberLifetime
{
    private string _title = "AuraEcho";
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly INavigationService _navigationService;
    private readonly IRegionDialogService _regionDialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IThemeManager _themeManager;
    private readonly IAppLogger _logger;
    private readonly IClientSession _clientSession;
    private readonly ITelemetryService _telemetry;
    private ObservableCollection<AppPlugin> _plugins;

    private readonly IPluginManager _pluginManager;

    public ObservableCollection<AppPlugin> Plugins
    {
        get => _plugins ??= [];
        set => SetProperty(ref _plugins, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DelegateCommand NavigationToPluginsMarketplaceCommand { get; }
    private async void NavigationToPluginsMarketplace()
    {
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.PluginsMarketplace);
    }

    public DelegateCommand NavigationToSettingsCommand { get; }
    private void NavigationToSettings()
    {
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.Settings);
    }

    public DelegateCommand LoadPluginsCommand { get; }
    private async void LoadPlugins()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var plugins = await _pluginManager.LoadPluginsAsync();
        stopwatch.Stop();

        Plugins = plugins.ToObservableCollection();
        _telemetry.TrackMetric("Plugin.LoadDuration", stopwatch.Elapsed.TotalMilliseconds, new Dictionary<string, string>
        {
            ["pluginCount"] = Plugins.Count.ToString()
        });

        _themeManager.AttachPluginThemes(Plugins);
    }

    public DelegateCommand<AppPlugin> PluginPlanUninstallCommand { get; }
    private async void PluginPlanUninstall(AppPlugin plugin)
    {
        _telemetry.TrackEvent("Plugin.UninstallPlanned", new Dictionary<string, string>
        {
            ["pluginId"] = plugin.PluginId.ToString(),
            ["pluginType"] = plugin.PluginType.ToString()
        });

        plugin.PlanStatus = PluginPlanStatus.UninstallPending;
        await _localPluginRepository.UpdateUserPluginStatusAsync(
            _clientSession.CurrentUser.Id,
            plugin.PluginId,
            plugin.PlanStatus);

        _eventAggregator.GetEvent<RequestRestartAppEvent>().Publish(new PluginUninstallPendingRestart
        {
            Id = Guid.NewGuid(),
            IconPath = Path.Combine(plugin.WorkingDirectory, plugin.Icon),
            PluginId = plugin.PluginId,
            Title = plugin.PluginName
        });

        RegionDialogResult dialogResult =
            await _regionDialogService.ShowDialogAsync(
                HostRegionNames.DialogRegion,
                ViewNames.ConfirmDialog,
                new NavigationParameters
                {
                    { "DialogArgs", new RegionDialogParameter
                    {
                        ConfirmText = Labels.Homepage_UninstallConfirmAck,
                        Message = Labels.Homepage_UninstallPendingMessage,
                        Title = Labels.Homepage_UninstallPending
                    }}
                });
    }

    public DelegateCommand<AppPlugin> CancelPluginPlanUninstallCommand { get; }
    private async void CancelPluginPlanUninstall(AppPlugin plugin)
    {
        if (plugin.PlanStatus != PluginPlanStatus.UninstallPending) return;

        _telemetry.TrackEvent("Plugin.UninstallCanceled", new Dictionary<string, string>
        {
            ["pluginId"] = plugin.PluginId.ToString()
        });

        plugin.PlanStatus = PluginPlanStatus.None;
        await _localPluginRepository.UpdateUserPluginStatusAsync(
            _clientSession.CurrentUser.Id,
            plugin.PluginId,
            plugin.PlanStatus);

        _eventAggregator.GetEvent<PluginCancelUninstallEvent>().Publish(plugin.PluginId);
    }

    public DelegateCommand<AppPlugin> SwitchPluginCommand { get; }
    public bool KeepAlive => false;

    private void SwitchPlugin(AppPlugin plugin)
    {
        if (plugin is null)
            return;

        _telemetry.TrackEvent("Plugin.Opened", new Dictionary<string, string>
        {
            ["pluginId"] = plugin.PluginId.ToString(),
            ["pluginType"] = plugin.PluginType.ToString()
        });

        switch (plugin.PluginType)
        {
            case PluginType.Native:
                _navigationService.RequestNavigate(
                    HostRegionNames.MainRegion,
                    (plugin as NativePlugin).PluginContext.EntryViewName,
                    new NavigationParameters
                    {
                        {  "PluginId", plugin.PluginId  },
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
            default: throw new NotImplementedException();
        }
    }

    public HomepageViewModel(
        INavigationService navigationService,
        ILocalPluginRepository localPluginRepository,
        IEventAggregator eventAggregator,
        IPluginManager pluginManager,
        IThemeManager themeManager,
        IAppLogger logger,
        IRegionDialogService regionDialogService,
        IClientSession clientSession,
        ITelemetryService telemetry)
    {
        _clientSession = clientSession;
        _navigationService = navigationService;
        _localPluginRepository = localPluginRepository;
        _eventAggregator = eventAggregator;
        _themeManager = themeManager;
        _logger = logger;
        _pluginManager = pluginManager;
        _regionDialogService = regionDialogService;
        _telemetry = telemetry;

        LoadPluginsCommand = new DelegateCommand(LoadPlugins);
        SwitchPluginCommand = new DelegateCommand<AppPlugin>(SwitchPlugin);
        NavigationToSettingsCommand = new DelegateCommand(NavigationToSettings);
        NavigationToPluginsMarketplaceCommand = new DelegateCommand(NavigationToPluginsMarketplace);
        PluginPlanUninstallCommand = new DelegateCommand<AppPlugin>(PluginPlanUninstall);
        CancelPluginPlanUninstallCommand = new DelegateCommand<AppPlugin>(CancelPluginPlanUninstall);

        _eventAggregator.GetEvent<PluginInstalledEvent>().Subscribe(AddNewPlugin);

        LoadPlugins();
    }

    private void AddNewPlugin(AppPlugin newPlugin)
    {
        if (Plugins.Any(p => p.PluginId == newPlugin.PluginId))
            return;

        Plugins.Add(newPlugin);
    }
}

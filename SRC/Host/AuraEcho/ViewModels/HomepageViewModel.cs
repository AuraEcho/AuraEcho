using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Strings;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
        var plugins = await _pluginManager.LoadPluginsAsync();
        Plugins = plugins.ToObservableCollection();

        _themeManager.AttachPluginThemes(Plugins);
    }

    public DelegateCommand<AppPlugin> PluginPlanUninstallCommand { get; }
    private async void PluginPlanUninstall(AppPlugin plugin)
    {
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
                HostRegionNames.ContentDialogRegion,
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
        IClientSession clientSession)
    {
        _clientSession = clientSession;
        _navigationService = navigationService;
        _localPluginRepository = localPluginRepository;
        _eventAggregator = eventAggregator;
        _themeManager = themeManager;
        _logger = logger;
        _pluginManager = pluginManager;
        _regionDialogService = regionDialogService;

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
        Plugins.Add(newPlugin);
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
namespace AuraEcho.ViewModels;

public class HomepageViewModel : BindableBase
{
    private string _title = "AuraEcho";
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly INavigationService _navigationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IThemeManager _themeManager;
    private readonly IAppLogger _logger;
    private ObservableCollection<PluginRegistryModel> _plugins;

    private readonly IPluginManager _pluginManager;

    public ObservableCollection<PluginRegistryModel> Plugins
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
    private void NavigationToPluginsMarketplace()
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
        var pluginRegistries = await _pluginManager.LoadPluginsAsync();
        Plugins = pluginRegistries.ToObservableCollection();

        _themeManager.AttachPluginThemes(
            pluginRegistries.Select(p => p.PluginContext)
                            .Where(p => p is not null));
    }

    public DelegateCommand<PluginRegistryModel> PluginPlanUninstallCommand { get; }
    private void PluginPlanUninstall(PluginRegistryModel plugin)
    {
        plugin.PlanStatus = PluginPlanStatus.UninstallPending;
        _localPluginRepository.UpdatePluginRegistry(plugin);
    }

    public DelegateCommand<PluginRegistryModel> CancelPluginPlanUninstallCommand { get; }
    private void CancelPluginPlanUninstall(PluginRegistryModel plugin)
    {
        if (plugin.PlanStatus != PluginPlanStatus.UninstallPending) return;

        plugin.PlanStatus = PluginPlanStatus.None;
        _localPluginRepository.UpdatePluginRegistry(plugin);
    }

    public DelegateCommand<PluginRegistryModel> SwitchPluginCommand { get; }

    private void SwitchPlugin(PluginRegistryModel pluginMetadata)
    {
        if (pluginMetadata is null)
            return;

        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            pluginMetadata.Manifest.DefaultViewName);
    }

    public HomepageViewModel(
        INavigationService navigationService, 
        ILocalPluginRepository localPluginRepository,
        IEventAggregator eventAggregator,
        IPluginManager pluginManager, 
        IThemeManager themeManager, 
        IAppLogger logger)
    {
        _navigationService = navigationService;
        _localPluginRepository = localPluginRepository;
        _eventAggregator = eventAggregator;
        _themeManager = themeManager;
        _logger = logger;
        _pluginManager = pluginManager;

        LoadPluginsCommand = new DelegateCommand(LoadPlugins);
        SwitchPluginCommand = new DelegateCommand<PluginRegistryModel>(SwitchPlugin);
        NavigationToSettingsCommand = new DelegateCommand(NavigationToSettings);
        NavigationToPluginsMarketplaceCommand = new DelegateCommand(NavigationToPluginsMarketplace);
        PluginPlanUninstallCommand = new DelegateCommand<PluginRegistryModel>(PluginPlanUninstall);
        CancelPluginPlanUninstallCommand = new DelegateCommand<PluginRegistryModel>(CancelPluginPlanUninstall);

        _eventAggregator.GetEvent<PluginInstalledEvent>().Subscribe(AddNewPlugin);

        LoadPlugins();
    }

    private void AddNewPlugin(PluginRegistryModel registry)
    {
        Plugins.Add(registry);
    }
}

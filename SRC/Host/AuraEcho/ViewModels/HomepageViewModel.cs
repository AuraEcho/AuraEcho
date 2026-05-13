using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
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
    private readonly IRegionDialogService _regionDialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IThemeManager _themeManager;
    private readonly IAppLogger _logger;
    private readonly IClientSession _clientSession;
    private ObservableCollection<UserPluginModel> _plugins;

    private readonly IPluginManager _pluginManager;

    public ObservableCollection<UserPluginModel> Plugins
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
        var pluginRegistries = await _pluginManager.LoadPluginsAsync();
        Plugins = pluginRegistries.ToObservableCollection();

        _themeManager.AttachPluginThemes(
            pluginRegistries.Select(p => p.PluginContext)
                            .Where(p => p is not null));
    }

    public DelegateCommand<UserPluginModel> PluginPlanUninstallCommand { get; }
    private void PluginPlanUninstall(UserPluginModel plugin)
    {
        plugin.Status = PluginPlanStatus.UninstallPending;
        _localPluginRepository.UpdateUserPluginStatusAsync(
            _clientSession.CurrentUser.Id,
            plugin.LocalPlugin.Id,
            plugin.Status);
    }

    public DelegateCommand<UserPluginModel> CancelPluginPlanUninstallCommand { get; }
    private void CancelPluginPlanUninstall(UserPluginModel plugin)
    {
        if (plugin.Status != PluginPlanStatus.UninstallPending) return;

        plugin.Status = PluginPlanStatus.None;
        _localPluginRepository.UpdateUserPluginStatusAsync(
            _clientSession.CurrentUser.Id,
            plugin.LocalPlugin.Id,
            plugin.Status);
    }

    public DelegateCommand<UserPluginModel> SwitchPluginCommand { get; }

    private void SwitchPlugin(UserPluginModel userPlugin)
    {
        if (userPlugin is null)
            return;

        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            userPlugin.PluginContext.EntryViewName,
            new NavigationParameters
            {
                {  "PluginId", userPlugin.LocalPlugin.Id  },
            });
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
        SwitchPluginCommand = new DelegateCommand<UserPluginModel>(SwitchPlugin);
        NavigationToSettingsCommand = new DelegateCommand(NavigationToSettings);
        NavigationToPluginsMarketplaceCommand = new DelegateCommand(NavigationToPluginsMarketplace);
        PluginPlanUninstallCommand = new DelegateCommand<UserPluginModel>(PluginPlanUninstall);
        CancelPluginPlanUninstallCommand = new DelegateCommand<UserPluginModel>(CancelPluginPlanUninstall);

        _eventAggregator.GetEvent<PluginInstalledEvent>().Subscribe(AddNewPlugin);

        LoadPlugins();
    }

    private void AddNewPlugin(UserPluginModel registry)
    {
        Plugins.Add(registry);
    }
}

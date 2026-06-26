using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Strings;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Models;

namespace AuraEcho.ViewModels;

public class SettingsViewModel : BindableBase
{
    #region private members
    private readonly IRegionManager _regionManager;
    private readonly IPluginManager _pluginManager;
    private readonly IAuthRepository _authRepository;
    private readonly IClientSession _clientSession;
    private readonly INavigationService _navigationService;
    private readonly IEventAggregator _eventAggregator;

    #endregion

    public ObservableCollection<AppSettingsItem> SettingsItems
    {
        get;
        set => SetProperty(ref field, value);
    }

    public AppSettingsItem CurrentSettingItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UserProfile CurrentUser
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand LoadSettingsCommand { get; }
    private void LoadSettings()
    {
        SettingsItems =
        [
            new HostSettingsItem()
            {
                Name = $"{nameof(Labels)}.{nameof(Labels.Settings_NavAccount)}",
                ViewName = ViewNames.AccountSettings
            },
            new HostSettingsItem()
            {
                Name = $"{nameof(Labels)}.{nameof(Labels.Settings_NavGeneral)}",
                ViewName = ViewNames.GeneralSettings
            }
        ];

        foreach (var plugin in _pluginManager.Plugins)
        {
            var pluginSettingsItem = plugin.GetSettings();
            if (pluginSettingsItem is null) continue;
            if (SettingsItems.Contains(pluginSettingsItem)) continue;

            SettingsItems.Add(pluginSettingsItem);
        }
        CurrentSettingItem = SettingsItems.First();
    }

    public DelegateCommand BackToHomeCommand { get; }
    private void BackToHome()
    {
        _regionManager.Regions[HostRegionNames.MainRegion].RemoveAll();
    }

    public DelegateCommand<string> NavigationToSettingsItemCommand { get; }
    private void NavigationToSettingsItem(string viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName)) return;

        _navigationService.RequestNavigate(HostRegionNames.SettingsContentRegion, viewName, canBack: false);
    }

    public DelegateCommand SignOutCommand { get; }
    private void SignOut()
    {
        _clientSession.SignOut();
        _regionManager.Regions[HostRegionNames.MainRegion].RemoveAll();

        _navigationService.Reset();

        _eventAggregator.GetEvent<AppRestartEvent>().Publish();
    }

    public SettingsViewModel(
        IRegionManager regionManager, 
        IPluginManager pluginManager, 
        IAuthRepository authRepository, 
        IClientSession clientSession,
        IEventAggregator eventAggregator,
        INavigationService navigationService)
    {
        _eventAggregator = eventAggregator;
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _clientSession = clientSession;

        NavigationToSettingsItemCommand = new DelegateCommand<string>(NavigationToSettingsItem);
        LoadSettingsCommand = new DelegateCommand(LoadSettings);
        BackToHomeCommand = new DelegateCommand(BackToHome);
        SignOutCommand = new DelegateCommand(SignOut);

        _ = LoadCurrentUserProfileAsync();
    }

    private async Task LoadCurrentUserProfileAsync()
    {
        var profile = await _authRepository.GetCurrentUserAsync();
        if (profile is null) return;

        CurrentUser = profile.ToUserProfile();
    }
}

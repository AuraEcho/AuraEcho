using AuraEcho.Api.Models.V1.Auth;
using AuraEcho.Constants;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class MainWindowViewModel : BindableBase
{
    #region private members
    private readonly IAuthRepository _authRepository;
    public IAuraToastService ToastService { get; }
    private readonly Task _autoSignInTask;
    private Version _currentVersion;
    #endregion

    public ObservableCollection<PendingRestartItem> PendingRestartItems
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public IClientSession ClientSession { get; }

    public INavigationService NavigationService
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private readonly IEventAggregator _eventAggregator;

    public DelegateCommand RequestRestartAppCommand { get; }
    private void RequestRestartApp()
    {
        _eventAggregator.GetEvent<AppRestartEvent>().Publish();
    }

    public DelegateCommand SignOutCommand { get; }
    private void SignOut()
    {
        ClientSession.SignOut();

        _eventAggregator.GetEvent<AppRestartEvent>().Publish();
    }

    public DelegateCommand NavigationToSettingsCommand { get; }
    private void NavigationToSettings()
    {
        NavigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.Settings);
    }

    public DelegateCommand GoBackCommand { get; }
    public bool CanGoBack() => NavigationService.CanGoBack;
    private void GoBack()
    {
        NavigationService.GoBack();
    }
     
    private void SignInExpired()
    {
        NavigationService.RequestNavigate(HostRegionNames.ContentDialogRegion, ViewNames.SignInExpired);
    }
    private void GoToTargetView(string viewName)
    {
        NavigationService.RequestNavigate(HostRegionNames.MainRegion, viewName);
    }

    public DelegateCommand AutoSignInCommand { get; }
    private async void AutoSignIn()
    {
        await _autoSignInTask;
        if (ClientSession.IsSignedIn)
        {
            NavigationService.RequestNavigate(HostRegionNames.HomeRegion, ViewNames.Homepage, canBack: false);
            return;
        }
        NavigationService.RequestNavigate(HostRegionNames.HomeRegion, ViewNames.SignIn, canBack: false);
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IAuthRepository authRepository,
        IClientSession clientSession,
        IAuraToastService auraToastService)
    {
        ToastService = auraToastService;
        NavigationService = navigationService;
        _eventAggregator = eventAggregator;
        _authRepository = authRepository;
        ClientSession = clientSession;

        GoBackCommand = new DelegateCommand(GoBack, CanGoBack);
        RequestRestartAppCommand = new DelegateCommand(RequestRestartApp);

        _eventAggregator.GetEvent<RequestViewEvent>().Subscribe(GoToTargetView);
        _eventAggregator.GetEvent<SignInExpiredEvent>().Subscribe(SignInExpired);
        _eventAggregator.GetEvent<RequestRestartAppEvent>().Subscribe(NewPendingRestartItem, ThreadOption.UIThread);
        AutoSignInCommand = new DelegateCommand(AutoSignIn);
        SignOutCommand = new DelegateCommand(SignOut);
        NavigationToSettingsCommand = new DelegateCommand(NavigationToSettings);
        if (NavigationService is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NavigationService.CanGoBack))
                    GoBackCommand.RaiseCanExecuteChanged();
            };
        }

        _autoSignInTask = AutoSignInAsync();
        _currentVersion = GetCurrentVersion();
    }

    private static Version GetCurrentVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"Software\AuraEcho");
            if (key is null) return new Version(0, 0, 0, 0);

            string? currentVersionStr = key.GetValue("CurrentVersion")?.ToString();
            if (String.IsNullOrWhiteSpace(currentVersionStr)) return new Version(0, 0, 0, 0);

            return Version.TryParse(currentVersionStr, out Version version) ? version : new Version(0, 0, 0, 0);
        }
        catch
        {
            return new Version(0, 0, 0, 0);
        }
    }

    private async Task AutoSignInAsync()
    {
        var refreshToken = SecureStore.Load(SecureStoreKeys.RefreshToken);
        if (refreshToken is null) return;

        var result = await _authRepository.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });

        if (result is null || result.Data is null)
        {
            SecureStore.Delete(SecureStoreKeys.RefreshToken);
            return;
        }

        ClientSession.SignIn(result.Data);
    }

    private void NewPendingRestartItem(PendingRestartItem newItem)
    {
        var existingItem = PendingRestartItems.FirstOrDefault(item => item.Id == newItem.Id);
        if (existingItem is null)
        {
            PendingRestartItems.Add(newItem);
            return;
        }

        existingItem.Name = newItem.Name;
        existingItem.Description = newItem.Description;
    }
}

using AuraEcho.Cloud.V1.Models.Order;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Strings;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class MainWindowViewModel : BindableBase
{
    #region private members
    private readonly IRegionDialogService _regionDialogService;
    public IAuraToastService ToastService { get; }
    private readonly ITokenProvider _tokenProvider;
    private readonly ISkuOrderCacheService _skuOrderCacheService;
    #endregion

    public Version CurrentVersion
    {
        get;
        set => SetProperty(ref field, value);
    }

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

    private void GoToTargetView(string viewName)
    {
        NavigationService.RequestNavigate(HostRegionNames.MainRegion, viewName);
    }

    public DelegateCommand AutoSignInCommand { get; }
    private async void AutoSignIn()
    {
        // TODO：用于解决登录界面的入场动画卡顿的问题，具体原理待研究
        await Task.Yield();
        if (_tokenProvider.RefreshToken is null)
        {
            NavigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SignIn, canBack: false);
            return;
        }

        NavigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.AutoSignIn, canBack: false);
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        ITokenProvider tokenProvider,
        IClientSession clientSession,
        IAuraToastService auraToastService,
        IRegionDialogService regionDialogService,
        ISkuOrderCacheService skuOrderCacheService)
    {
        _regionDialogService = regionDialogService;
        _skuOrderCacheService = skuOrderCacheService;
        ToastService = auraToastService;
        NavigationService = navigationService;
        _eventAggregator = eventAggregator;
        ClientSession = clientSession;
        _tokenProvider = tokenProvider;

        GoBackCommand = new DelegateCommand(GoBack, CanGoBack);
        RequestRestartAppCommand = new DelegateCommand(RequestRestartApp);

        _eventAggregator.GetEvent<RequestViewEvent>().Subscribe(GoToTargetView);
        _eventAggregator.GetEvent<SignInExpiredEvent>().Subscribe(SignInExpired);
        _eventAggregator.GetEvent<KickedOutEvent>().Subscribe(KickedOut);
        _eventAggregator.GetEvent<RequestRestartAppEvent>().Subscribe(NewPendingRestartItem, ThreadOption.UIThread);
        _eventAggregator.GetEvent<PluginCancelUninstallEvent>().Subscribe(PluginCancelUninstall, ThreadOption.UIThread);
        _eventAggregator.GetEvent<OrderPaidEvent>().Subscribe(OrderPid);
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

        GetCurrentVersionAsync();
    }

    private async void OrderPid(OrderPaymentDetails details)
    {
        _skuOrderCacheService.InvalidateCache(details.ResourceId, details.SkuId, details.PaymentMethod);
    }

    private async void KickedOut()
    {
        ClientSession.SignOut();

        RegionDialogResult dialogResult =
            await _regionDialogService.ShowDialogAsync(
                HostRegionNames.DialogRegion,
                ViewNames.ConfirmDialog,
                new NavigationParameters
                {
                    { "DialogArgs", new RegionDialogParameter
                    {
                        CancelText = Labels.MainWindow_ReLogin,
                        ConfirmText = Labels.App_Exit,
                        Message = Labels.MainWindow_KickedOutMessage,
                        Title = Labels.MainWindow_KickedOutTitle
                    }}
                });

        if (dialogResult != RegionDialogResult.Cancel)
        {
            _eventAggregator.GetEvent<AppShutdownEvent>().Publish();
            return;
        }

        _eventAggregator.GetEvent<AppRestartEvent>().Publish();
    }

    private async void SignInExpired()
    {
        RegionDialogResult dialogResult =
            await _regionDialogService.ShowDialogAsync(
                HostRegionNames.DialogRegion,
                ViewNames.ConfirmDialog,
                new NavigationParameters
                {
                    { "DialogArgs", new RegionDialogParameter
                    {
                        CancelText = Labels.MainWindow_SignInExpiredCancel,
                        ConfirmText = Labels.MainWindow_ReLogin,
                        Message = Labels.MainWindow_SignInExpiredMessage,
                        Title = Labels.MainWindow_SignInExpiredTitle
                    }}
                });

        if (dialogResult == RegionDialogResult.OK)
        {
            _eventAggregator.GetEvent<AppRestartEvent>().Publish();
            return;
        }
    }

    private void PluginCancelUninstall(Guid pluginId)
    {
        PluginUninstallPendingRestart? targetUninstallItem =
            PendingRestartItems.OfType<PluginUninstallPendingRestart>()
                               .FirstOrDefault(item => item.PluginId == pluginId);
        PendingRestartItems.Remove(targetUninstallItem);
    }

    private async void GetCurrentVersionAsync()
    {
        CurrentVersion = await Task.Run(() => GetCurrentVersion());

        static Version GetCurrentVersion()
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
    }

    private void NewPendingRestartItem(PendingRestartItem newItem)
    {
        switch (newItem)
        {
            case AppUpdatePendingRestart au:
                PendingRestartItems.OfType<AppUpdatePendingRestart>()
                                   .ToList()
                                   .ForEach(item => PendingRestartItems.Remove(item));
                break;
            case PluginUpdatePendingRestart pu:
                PluginUpdatePendingRestart? targetItem =
                    PendingRestartItems.OfType<PluginUpdatePendingRestart>()
                                       .FirstOrDefault(item => item.PluginId == pu.PluginId);
                PendingRestartItems.Remove(targetItem);
                break;
            case PluginUninstallPendingRestart pup:
                PluginUninstallPendingRestart? targetUninstallItem =
                    PendingRestartItems.OfType<PluginUninstallPendingRestart>()
                                       .FirstOrDefault(item => item.PluginId == pup.PluginId);
                PendingRestartItems.Remove(targetUninstallItem);
                break;
            default:
                break;
        }

        PendingRestartItems.Add(newItem);
    }
}

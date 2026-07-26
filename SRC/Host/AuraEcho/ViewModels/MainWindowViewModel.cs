using AuraEcho.Telemetry;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Activity;
using AuraEcho.Cloud.V1.Models.Order;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Services;
using AuraEcho.Strings;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class MainWindowViewModel : BindableBase
{
    #region private members
    private readonly IRegionDialogService _regionDialogService;
    public IAuraToastService ToastService { get; }
    private readonly ITokenProvider _tokenProvider;
    private readonly OrderPayUrlCacheService _orderPayUrlCacheService;
    private readonly ITelemetryService _telemetry;
    private readonly ApiClient _apiClient;
    private readonly TelemetryContextFactory _contextFactory;
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

    public IAnnouncementService AnnouncementService { get; }

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

    public DelegateCommand NavigationToSendFeedbackCommand { get; }
    private void NavigationToSendFeedback()
    {
        NavigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SendFeedback);
    }

    public DelegateCommand ShowAnnouncementsCommand { get; }
    private void ShowAnnouncements()
    {
        NavigationService.RequestNavigate(HostRegionNames.DialogRegion, ViewNames.AnnouncementView, canBack: false);
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
        OrderPayUrlCacheService orderPayUrlCacheService,
        ITelemetryService telemetry,
        ApiClient apiClient,
        TelemetryContextFactory contextFactory,
        IAnnouncementService announcementService)
    {
        _regionDialogService = regionDialogService;
        AnnouncementService = announcementService;
        ToastService = auraToastService;
        NavigationService = navigationService;
        _eventAggregator = eventAggregator;
        ClientSession = clientSession;
        _tokenProvider = tokenProvider;
        _orderPayUrlCacheService = orderPayUrlCacheService;
        _telemetry = telemetry;
        _apiClient = apiClient;
        _contextFactory = contextFactory;

        GoBackCommand = new DelegateCommand(GoBack, CanGoBack);
        RequestRestartAppCommand = new DelegateCommand(RequestRestartApp);

        _eventAggregator.GetEvent<RequestViewEvent>().Subscribe(GoToTargetView);
        _eventAggregator.GetEvent<SignedInEvent>().Subscribe(OnSignedIn);
        _eventAggregator.GetEvent<SignInExpiredEvent>().Subscribe(SignInExpired);
        _eventAggregator.GetEvent<KickedOutEvent>().Subscribe(KickedOut, ThreadOption.UIThread);
        _eventAggregator.GetEvent<RequestRestartAppEvent>().Subscribe(NewPendingRestartItem, ThreadOption.UIThread);
        _eventAggregator.GetEvent<PluginCancelUninstallEvent>().Subscribe(PluginCancelUninstall, ThreadOption.UIThread);
        _eventAggregator.GetEvent<OrderPaidEvent>().Subscribe(OrderPaid);
        AutoSignInCommand = new DelegateCommand(AutoSignIn);
        SignOutCommand = new DelegateCommand(SignOut);
        NavigationToSettingsCommand = new DelegateCommand(NavigationToSettings);
        NavigationToSendFeedbackCommand = new DelegateCommand(NavigationToSendFeedback);
        ShowAnnouncementsCommand = new DelegateCommand(ShowAnnouncements);
        if (NavigationService is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NavigationService.CanGoBack))
                    GoBackCommand.RaiseCanExecuteChanged();
            };
        }

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
    }

    private async void OrderPaid(OrderPaymentDetails details)
    {
        _orderPayUrlCacheService.Remove(new(details.SkuId, details.PaymentMethod));
    }

    private void OnSignedIn()
    {
        _ = ReportDauAsync();
    }

    private async Task ReportDauAsync()
    {
        try
        {
            await _apiClient.Activity.ReportAsync(new ActivityReportRequest
            {
                SessionId = _contextFactory.SessionId,
                ClientVersion = _contextFactory.Context.AppVersion
            });
        }
        catch
        {
            // 静默失败
        }
    }

    private async void KickedOut()
    {
        _telemetry.TrackEvent("Auth.KickedOut");
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
        _telemetry.TrackEvent("Auth.SessionExpired");

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

using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Cloud.V1.Models.License;
using AuraEcho.Cloud.V1.Models.Plugin;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Enums;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Telemetry;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class SubscriptionsViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly ApiClient _apiClient;
    private readonly IPurchaseCoordinator _purchaseCoordinator;
    private readonly ITelemetryService _telemetry;
    private readonly INavigationService _navigationService;
    private readonly IClock _clock;

    public LoadState LoadState
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<SubscriptionItem> Subscriptions
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            RaisePropertyChanged(nameof(HasSubscriptions));
        }
    }

    public bool HasSubscriptions => Subscriptions?.Count > 0;

    public bool KeepAlive => false;

    public DelegateCommand LoadSubscriptionsCommand { get; }
    private async void LoadSubscriptionsAsync()
    {
        await ReloadAsync();
    }

    public DelegateCommand NavigateToPluginsMarketplaceCommand { get; }
    private void NavigateToPluginsMarketplace()
    {
        _navigationService.RequestNavigate(
            HostRegionNames.MainRegion,
            ViewNames.PluginsMarketplace);
    }

    public DelegateCommand RefreshCommand { get; }
    private async void RefreshAsync()
    {
        if (LoadState == LoadState.Loading) return;

        _telemetry.TrackEvent("Subscriptions.Refresh");
        await ReloadAsync();
    }

    public DelegateCommand<SubscriptionItem> RenewSubscriptionCommand { get; }
    private async void RenewSubscriptionAsync(SubscriptionItem item)
    {
        if (item is null) return;

        _telemetry.TrackEvent("Subscriptions.RenewClicked", new Dictionary<string, string>
        {
            ["resourceId"] = item.ResourceId.ToString(),
            ["isExpired"] = item.IsExpired.ToString()
        });

        bool purchased = await _purchaseCoordinator.RequestPurchaseAsync(item.ResourceId);
        if (purchased) await ReloadAsync();
    }

    public SubscriptionsViewModel(
        ApiClient apiClient,
        IPurchaseCoordinator purchaseCoordinator,
        INavigationService navigationService,
        ITelemetryService telemetry,
        IClock clock)
    {
        _navigationService = navigationService;
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _purchaseCoordinator = purchaseCoordinator ?? throw new ArgumentNullException(nameof(purchaseCoordinator));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));

        LoadSubscriptionsCommand = new DelegateCommand(LoadSubscriptionsAsync);
        RefreshCommand = new DelegateCommand(RefreshAsync);
        RenewSubscriptionCommand = new DelegateCommand<SubscriptionItem>(RenewSubscriptionAsync);
        NavigateToPluginsMarketplaceCommand = new DelegateCommand(NavigateToPluginsMarketplace);
    }

    private async Task ReloadAsync()
    {
        LoadState = LoadState.Loading;

        try
        {
            var minTimeTask = Task.Delay(TimeSpan.FromSeconds(0.5));

            ResponseResult<List<LicenseResponseItem>> response =
                await _apiClient.License.GetUserLicensesAsync();

            if (response?.Status != ResultStatus.Success || response.Data is null)
                throw new Exception($"GetUserLicenses failed: {response?.Status}");

            List<LicenseResponseItem> licenses =
                response.Data.Where(l => l.ExpiredAt.HasValue).ToList();

            SubscriptionItem[] items = await Task.WhenAll(licenses.Select(BuildItemAsync));

            // 即将到期优先，其次正常，已过期沉底
            List<SubscriptionItem> ordered =
                items
                .Where(i => i is not null)
                .OrderBy(i => i.IsExpired ? 2 : i.IsExpiringSoon ? 0 : 1)
                .ThenBy(i => i.ExpiredAt)
                .ToList();

            Subscriptions = new ObservableCollection<SubscriptionItem>(ordered);

            await minTimeTask;
            LoadState = LoadState.Loaded;

            _telemetry.TrackEvent("Subscriptions.Loaded", new Dictionary<string, string>
            {
                ["total"] = ordered.Count.ToString(),
                ["expiringSoon"] = ordered.Count(i => i.IsExpiringSoon).ToString(),
                ["expired"] = ordered.Count(i => i.IsExpired).ToString()
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Subscriptions] Load failed: {ex}");
            LoadState = LoadState.Failed;
            _telemetry.TrackEvent("Subscriptions.LoadFailed", new Dictionary<string, string>
            {
                ["reason"] = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// 创建订阅项
    /// </summary>
    private async Task<SubscriptionItem> BuildItemAsync(LicenseResponseItem license)
    {
        GetPluginByIdResult plugin = null;
        try
        {
            plugin = await _apiClient.Plugin.GetPluginByIdAsync(license.ResourceId);
        }
        catch (Exception ex)
        {
            // 单个插件查询失败不应让整页失败，降级为仅展示授权信息
            Debug.WriteLine($"[Subscriptions] Fetch plugin {license.ResourceId} failed: {ex}");
        }

        return new SubscriptionItem
        {
            ResourceId = license.ResourceId,
            PluginName = plugin?.Name ?? license.ResourceId.ToString(),
            PluginIconUrl = plugin?.IconFileUrl,
            TierName = license.TierName,
            TierDescription = license.TierDescription,
            ExpiredAt = license.ExpiredAt,
            Now = _clock.UtcNow.UtcDateTime
        };
    }
}

using AuraEcho.Telemetry;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Cloud.V1.Models.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Enums;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Services;
using AuraEcho.Strings;
using AuraEcho.Toolkit.Wpf.RegionDialog;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class PurchaseViewModel : BindableBase, IRegionDialogAware
{
    private Guid _resourceId;
    private readonly IEventAggregator _eventAggregator;
    private readonly ApiClient _apiClient;
    private readonly IAuraToastService _auraToastService;
    private readonly OrderCacheService _orderPayUrlCacheService;
    private readonly IClock _clock;
    private readonly ITelemetryService _telemetry;

    private const string QRCODE_PLACEHOLDER_TEXT = "QRPlaceHolderQRPlaceHolderQRPlaceHolder";
    private int _currentOrderTaskId;
    private bool _isSettled;
    private readonly CancellationTokenSource _orderStatusTaskToken = new();

    public PurchaseState State
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsInitializing
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public RemotePlugin CurrentPlugin
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ResourceLicense CurrentPluginLicense
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<Sku> Skus
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<SkuTierGroup> TierGroups
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Sku SelectedSku
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(DisplayPayableAmount));
            if (value is null) return;
            if (IsInitializing) return;

            RefreshOrderAsync();
        }
    }

    public SkuTierGroup SelectedTierGroup
    {
        get;
        set
        {
            // 降级组已在 UI 上禁用，此处兜住命令与初始化路径，避免发起注定失败的下单
            if (value is { IsPurchasable: false }) return;
            if (!SetProperty(ref field, value)) return;
            if (IsInitializing) return;
            if (value?.Skus?.Count > 0)
            {
                SelectedSku = value.Skus.First();
            }
        }
    }

    public PaymentChannel PaymentChannel
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            RefreshOrderAsync();
        }
    } = PaymentChannel.Wxpay;

    public string QRCode
    {
        get;
        set => SetProperty(ref field, value);
    } = QRCODE_PLACEHOLDER_TEXT;

    /// <summary>
    /// 当前 SKU 的下单报价（售价、抵扣、应付金额等）
    /// </summary>
    public CreateOrderResponse CurrentOrder
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(DisplayPayableAmount));
        }
    }

    /// <summary>
    /// 结算区展示的金额。报价未就绪时回退到 SKU 售价。
    /// </summary>
    public decimal DisplayPayableAmount =>
        CurrentOrder?.PayableAmount ?? SelectedSku?.SalePrice ?? 0m;

    public OrderSettlement PaymentResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 本次开通是否为零元订单。零元订单无支付渠道，成功页不展示支付方式。
    /// </summary>
    public bool IsFreeOrder
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand<Sku> SelectSkuCommand { get; }
    private void SelectSku(Sku sku)
    {
        SelectedSku = sku;
    }

    public DelegateCommand<SkuTierGroup> SelectTierCommand { get; }
    private void SelectTier(SkuTierGroup tierGroup)
    {
        SelectedTierGroup = tierGroup;
    }

    public DelegateCommand RefreshOrderCommand { get; }
    private async void RefreshOrderAsync()
    {
        if (SelectedSku is null) return;

        await CreateOrderAsync();
    }

    public DelegateCommand OkCommand { get; }
    private void Ok()
    {
        _orderStatusTaskToken.Cancel();
        RequestClose?.Invoke(RegionDialogResult.OK);
    }

    public DelegateCommand CancelCommand { get; }
    private void Cancel()
    {
        TrackDialogClosed("cancel");
        _orderStatusTaskToken.Cancel();
        RequestClose?.Invoke(RegionDialogResult.Cancel);
    }

    public DelegateCommand CloseCommand { get; }
    private void Close()
    {
        TrackDialogClosed("close");
        _orderStatusTaskToken.Cancel();
        RequestClose?.Invoke(RegionDialogResult.Close);
    }

    /// <summary>
    /// 记录购买对话框关闭。未支付即关闭视为转化流失。
    /// </summary>
    private void TrackDialogClosed(string trigger)
    {
        if (State == PurchaseState.Paid) return;
        _telemetry.TrackEvent("Purchase.DialogClosedUnpaid", new Dictionary<string, string>
        {
            ["resourceId"] = _resourceId.ToString(),
            ["trigger"] = trigger
        });
    }

    public event Action<RegionDialogResult> RequestClose;

    public PurchaseViewModel(
        IClock clock,
        IEventAggregator eventAggregator,
        ApiClient apiClient,
        IAuraToastService auraToastService,
        OrderCacheService orderPayUrlCacheService,
        ITelemetryService telemetry)
    {
        _clock = clock;
        _eventAggregator = eventAggregator;
        _apiClient = apiClient;
        _auraToastService = auraToastService;
        _orderPayUrlCacheService = orderPayUrlCacheService;
        _telemetry = telemetry;

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(Cancel);
        CloseCommand = new DelegateCommand(Close);
        SelectSkuCommand = new DelegateCommand<Sku>(SelectSku);
        SelectTierCommand = new DelegateCommand<SkuTierGroup>(SelectTier);
        RefreshOrderCommand = new DelegateCommand(RefreshOrderAsync);
        ConfirmFreeOrderCommand = new DelegateCommand(ConfirmFreeOrderAsync);

        _eventAggregator.GetEvent<OrderSettledEvent>().Subscribe(OnOrderSettled);
    }

    public async void OnDialogOpened(NavigationParameters parameters)
    {
        _resourceId = parameters.GetValue<Guid>("ResourceId");
        _telemetry.TrackEvent("Purchase.DialogOpened", new Dictionary<string, string>
        {
            ["resourceId"] = _resourceId.ToString()
        });
        await InitializeAsync(_resourceId);
    }

    private async Task InitializeAsync(Guid resourceId)
    {
        try
        {
            State = PurchaseState.Loading;

            var minTimeTask = Task.Delay(TimeSpan.FromSeconds(.5));

            var tPlugin = _apiClient.Plugin.GetPluginByIdAsync(resourceId);
            var tSkus = _apiClient.Sku.GetResourceSkusAsync(resourceId);
            var tLicense = GetLicenseInfo(resourceId);
            await Task.WhenAll(tPlugin, tSkus, tLicense);

            CurrentPlugin = tPlugin.Result?.ToRemotePlugin();
            CurrentPluginLicense = tLicense.Result;
            var skuList = tSkus.Result?.Skus?.Select(s => s.ToSku()).Where(s => s.IsActive).OrderBy(s => s.Ordinal);
            Skus = new ObservableCollection<Sku>(skuList ?? Enumerable.Empty<Sku>());

            // 按 LicenseTierId 构建等级分组，同等级下按时长排序
            var groups = Skus
                .GroupBy(s => s.LicenseTierId)
                .OrderBy(g => g.Min(s => s.TierLevel))
                .Select(g => new SkuTierGroup
                {
                    TierName = g.First().TierName,
                    TierLevel = g.Min(s => s.TierLevel),
                    Skus = new ObservableCollection<Sku>(g.OrderBy(s => s.DurationMonths))
                })
                .ToList();

            // 低于当前生效等级的组为降级，服务端会拒绝下单，此处直接锁定
            if (CurrentPluginLicense?.TierLevel is int currentTierLevel)
            {
                foreach (SkuTierGroup group in groups.Where(g => g.TierLevel < currentTierLevel))
                {
                    group.IsPurchasable = false;
                    group.LockReason = string.Format(
                        CultureInfo.CurrentCulture,
                        Labels.Purchase_DowngradeLockedTip,
                        CurrentPluginLicense.TierName);
                }
            }

            TierGroups = new ObservableCollection<SkuTierGroup>(groups);
            SelectedTierGroup = PickDefaultTierGroup(groups, CurrentPluginLicense?.TierLevel);

            // 无可购买项（无上架 SKU / 等级全部低于当前订阅 / 该等级无上架时长），
            if (SelectedTierGroup is null || SelectedTierGroup.Skus.Count == 0)
            {
                State = PurchaseState.Ready;
                await minTimeTask;
                return;
            }

            SelectedSku = SelectedTierGroup.Skus.First();
            await CreateOrderAsync();

            await minTimeTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Purchase] Init load failed: {ex}");
            _auraToastService.Show(Labels.Purchase_QRCodeGenerateFailed, ToastLevel.Error);
            State = PurchaseState.OrderFailed;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// 选出默认等级组
    /// </summary>
    private static SkuTierGroup? PickDefaultTierGroup(List<SkuTierGroup> groups, int? currentTierLevel)
    {
        if (currentTierLevel is int level)
        {
            SkuTierGroup? renewal = groups.FirstOrDefault(g => g.TierLevel == level);
            if (renewal is not null) return renewal;
        }

        return groups.FirstOrDefault(g => g.IsPurchasable);
    }

    private async Task<bool> CreateOrderAsync()
    {
        if (SelectedSku is null) return false;

        State = PurchaseState.CreatingOrder;

        var stopwatch = Stopwatch.StartNew();
        var task = GetOrFetchOrderAsync(new OrderPayUrlCacheKey(SelectedSku.Id, PaymentChannel));

        int taskId = task.Id;
        _currentOrderTaskId = taskId;
        await Task.WhenAll(task, Task.Delay(TimeSpan.FromSeconds(0.3)));

        if (_currentOrderTaskId != taskId) return false;

        stopwatch.Stop();
        (ResultStatus resultStatus, CreateOrderResponse? order, string? message) = task.Result;
        if (resultStatus == ResultStatus.Success && order is not null)
        {
            CurrentOrder = order;

            // 应付金额为 0 时服务端不向支付渠道下单，需用户确认后调 confirm-free 开通
            if (order.NeedPayment)
            {
                QRCode = order.QRCode;
                State = PurchaseState.Ready;
            }
            else
            {
                QRCode = QRCODE_PLACEHOLDER_TEXT;
                State = PurchaseState.ConfirmPending;
            }

            return true;
        }

        CurrentOrder = null;
        State = PurchaseState.OrderFailed;
        _telemetry.TrackEvent("Purchase.OrderFailed", new Dictionary<string, string>
        {
            ["skuId"] = SelectedSku.Id.ToString(),
            ["channel"] = PaymentChannel.ToString(),
            ["reason"] = resultStatus.ToString()
        });
        // 降级是业务规则而非技术故障。客户端已前置拦截，此处兜住 license 快照过期的情况
        _auraToastService.Show(
            resultStatus == ResultStatus.DowngradeNotAllowed
                ? Labels.Purchase_DowngradeNotAllowed
                : Labels.Purchase_QRCodeGenerateFailed,
            ToastLevel.Error);
        return false;

        async Task<(ResultStatus Status, CreateOrderResponse? Order, string? Message)> GetOrFetchOrderAsync(OrderPayUrlCacheKey key)
        {
            if (_orderPayUrlCacheService.TryGet(key, out CreateOrderResponse? cached) && cached is not null)
                return (ResultStatus.Success, cached, null);

            ResponseResult<CreateOrderResponse> response =
                await _apiClient.Order.CreateOrderAsync(new CreateOrderRequest
                {
                    SkuId = SelectedSku.Id,
                    Channel = PaymentChannel
                });

            if (response is null) return (ResultStatus.OrderCreationFailed, null, null);

            if (response.Status == ResultStatus.Success && response.Data is not null)
            {
                _orderPayUrlCacheService.Create(key, response.Data);
                return (response.Status, response.Data, null);
            }

            return (response.Status, null, response.Message);
        }
    }

    public DelegateCommand ConfirmFreeOrderCommand { get; }
    private async void ConfirmFreeOrderAsync()
    {
        if (CurrentOrder is null || CurrentOrder.NeedPayment) return;
        if (State == PurchaseState.Confirming) return;
        if (SelectedSku is null) return;

        Guid orderId = CurrentOrder.OrderId;
        // await 期间用户可能切换 SKU/渠道，先固定当前上下文
        Guid skuId = SelectedSku.Id;
        var cacheKey = new OrderPayUrlCacheKey(skuId, PaymentChannel);
        State = PurchaseState.Confirming;

        try
        {
            ResponseResult<ConfirmFreeOrderResult>? response =
                await _apiClient.Order.ConfirmFreeOrderAsync(orderId);

            if (response?.Status == ResultStatus.Success && response.Data?.IsProvisioned == true)
            {
                // 报价已消耗，避免下次进入时复用同一订单
                _orderPayUrlCacheService.Remove(cacheKey);
                _telemetry.TrackEvent("Purchase.FreeOrderConfirmed", new Dictionary<string, string>
                {
                    ["resourceId"] = _resourceId.ToString(),
                    ["skuId"] = skuId.ToString(),
                    ["orderId"] = orderId.ToString()
                });

                // confirm-free 响应不含订单号与开通时间，回查订单补全成功页展示
                GetOrderByIdResult detail = null;
                try { detail = await _apiClient.Order.GetOrderByIdAsync(orderId); }
                catch (Exception ex) { Debug.WriteLine($"[Purchase] Fetch free order detail failed: {ex}"); }

                // 与扫码支付走同一展示路径。服务端也会推送 OrderSettled，
                // 但本机不依赖推送到达，直接用同步响应构造终局。
                OnOrderSettled(new OrderSettlement
                {
                    OrderId = orderId,
                    OrderNumber = detail?.OrderNumber ?? String.Empty,
                    Status = OrderStatus.Paid,
                    Kind = CurrentOrder.Kind,
                    ResourceId = _resourceId,
                    SkuId = skuId,
                    PayableAmount = 0m,
                    CreditAmount = CurrentOrder.CreditAmount,
                    BonusDays = CurrentOrder.BonusDays,
                    PaidAmount = 0m,
                    // 零元订单无支付渠道
                    PaymentMethod = null,
                    // 与扫码支付推送的 PayTime 保持同一处理方式，不额外做时区换算
                    PayTime = detail?.PayTime ?? _clock.UtcNow.UtcDateTime
                });
                return;
            }

            // 开通失败通常是下单后授权链已变化，报价失效，需重新下单
            _telemetry.TrackEvent("Purchase.FreeOrderConfirmFailed", new Dictionary<string, string>
            {
                ["resourceId"] = _resourceId.ToString(),
                ["orderId"] = orderId.ToString(),
                ["reason"] = response?.Status.ToString() ?? "NoResponse"
            });
            _auraToastService.Show(Labels.Purchase_ConfirmFreeFailed, ToastLevel.Error);
            _orderPayUrlCacheService.Remove(cacheKey);
            await CreateOrderAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Purchase] Confirm free order failed: {ex}");
            _auraToastService.Show(Labels.Purchase_ConfirmFreeFailed, ToastLevel.Error);
            _orderPayUrlCacheService.Remove(cacheKey);
            State = PurchaseState.ConfirmPending;
        }
    }


    private async Task<ResourceLicense> GetLicenseInfo(Guid pluginId)
    {
        var response = await _apiClient.License.GetResourceLicenseAsync(pluginId);
        var license = response?.ToResourceLicense();
        if (license is null || !license.IsValid || license.ExpiredAt < _clock.UtcNow)
            return null;
        return license;
    }

    /// <summary>
    /// 订单结果处理
    /// </summary>
    private void OnOrderSettled(OrderSettlement settlement)
    {
        // 仅处理当前资源的订单结果
        if (settlement.ResourceId != _resourceId) return;

        // 订单结果只处理一次
        _eventAggregator.GetEvent<OrderSettledEvent>().Unsubscribe(OnOrderSettled);

        Debug.WriteLine($"OnOrderSettled: {settlement.Status}");

        PaymentResult = settlement;

        if (settlement.Status is OrderStatus.Paid)
        {
            // 零元开通与扫码支付共用此路径，按实付金额区分成功页展示
            IsFreeOrder = settlement.PaidAmount is null or <= 0m;
            State = PurchaseState.Paid;
            _telemetry.TrackEvent("Purchase.Paid", new Dictionary<string, string>
            {
                ["resourceId"] = _resourceId.ToString(),
                ["skuId"] = settlement.SkuId.ToString(),
                ["kind"] = settlement.Kind.ToString()
            });
            return;
        }
        
        // TODO: 应该区分退款/和退款中两种状态

        // 已收款但无法交付授权，服务端已转入退款
        State = PurchaseState.Refunding;
        _telemetry.TrackEvent("Purchase.Refunding", new Dictionary<string, string>
        {
            ["resourceId"] = _resourceId.ToString(),
            ["skuId"] = settlement.SkuId.ToString(),
            ["status"] = settlement.Status.ToString(),
            ["reason"] = settlement.RefundReason.ToString()
        });
    }
}

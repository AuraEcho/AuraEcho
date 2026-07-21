using AuraEcho.Telemetry;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Cloud.V1.Models.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Enums;
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
    private readonly OrderPayUrlCacheService _orderPayUrlCacheService;
    private readonly IClock _clock;
    private readonly ITelemetryService _telemetry;

    private int _currentOrderTaskId;
    private bool _isPaid;
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

    public Sku SelectedSku
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            if (value is null) return;
            if (IsInitializing) return;

            RefreshOrderAsync();
        }
    }

    public PaymentChannel PaymentChannel
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            if (!IsInitializing)
                _telemetry.TrackEvent("Purchase.PaymentChannelChanged", new Dictionary<string, string>
                {
                    ["channel"] = value.ToString()
                });

            RefreshOrderAsync();
        }
    } = PaymentChannel.Wxpay;

    public string QRCode
    {
        get;
        set => SetProperty(ref field, value);
    } = "QRPlaceHolderQRPlaceHolderQRPlaceHolder";

    public OrderPaymentDetails PaymentResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand<Sku> SelectSkuCommand { get; }
    private void SelectSku(Sku sku)
    {
        SelectedSku = sku;
        if (sku is not null && !IsInitializing)
            _telemetry.TrackEvent("Purchase.SkuSelected", new Dictionary<string, string>
            {
                ["skuId"] = sku.Id.ToString(),
                ["resourceId"] = _resourceId.ToString()
            });
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
        if (_isPaid) return;
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
        OrderPayUrlCacheService orderPayUrlCacheService,
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
        RefreshOrderCommand = new DelegateCommand(RefreshOrderAsync);

        _eventAggregator.GetEvent<OrderPaidEvent>().Subscribe(OnOrderPaid);
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

            if (Skus.Count == 0)
            {
                State = PurchaseState.Ready;
                return;
            }

            SelectedSku = Skus.First();

            if (SelectedSku is not null)
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

    private async Task<bool> CreateOrderAsync()
    {
        if (SelectedSku is null) return false;

        State = PurchaseState.CreatingOrder;

        var stopwatch = Stopwatch.StartNew();
        var task = GetOrFetchPayUrlAsync(new OrderPayUrlCacheKey(SelectedSku.Id, PaymentChannel)); ;

        int taskId = task.Id;
        _currentOrderTaskId = taskId;
        await Task.WhenAll(task, Task.Delay(TimeSpan.FromSeconds(0.3)));

        if (_currentOrderTaskId != taskId) return false;

        stopwatch.Stop();
        (ResultStatus resultStatus, string? payUrl) = task.Result;
        if (resultStatus == ResultStatus.Success)
        {
            QRCode = payUrl;
            State = PurchaseState.Ready;
            _telemetry.TrackEvent("Purchase.OrderCreated", new Dictionary<string, string>
            {
                ["skuId"] = SelectedSku.Id.ToString(),
                ["channel"] = PaymentChannel.ToString()
            });
            _telemetry.TrackMetric("Purchase.OrderCreateDuration", new Dictionary<string, double> { ["value"] = stopwatch.Elapsed.TotalMilliseconds });
            return true;
        }

        State = PurchaseState.OrderFailed;
        _telemetry.TrackEvent("Purchase.OrderFailed", new Dictionary<string, string>
        {
            ["skuId"] = SelectedSku.Id.ToString(),
            ["channel"] = PaymentChannel.ToString(),
            ["reason"] = resultStatus.ToString()
        });
        _auraToastService.Show(
            Labels.Purchase_QRCodeGenerateFailed,
            ToastLevel.Error);
        return false;

        async Task<string?> OrderPayUrlFetcher(OrderPayUrlCacheKey key)
        {
            ResponseResult<CreateOrderResponse> response =
                await _apiClient.Order.CreateOrderAsync(new CreateOrderRequest
                {
                    SkuId = SelectedSku.Id,
                    Channel = PaymentChannel
                });

            return response?.Status == ResultStatus.Success && response.Data is not null
            ? response.Data.PayUrl
            : null;
        }

        async Task<(ResultStatus Status, string? PayUrl)> GetOrFetchPayUrlAsync(OrderPayUrlCacheKey key)
        {
            if (_orderPayUrlCacheService.TryGet(key, out string? payUrl))
                return (ResultStatus.Success, payUrl);

            ResponseResult<CreateOrderResponse> response =
                await _apiClient.Order.CreateOrderAsync(new CreateOrderRequest
                {
                    SkuId = SelectedSku.Id,
                    Channel = PaymentChannel
                });

            if (response is null) return (ResultStatus.OrderCreationFailed, null);

            if (response.Status == ResultStatus.Success && response.Data is not null)
            {
                _orderPayUrlCacheService.Create(key, response.Data.QRCode);
                return (response.Status, response.Data.QRCode);
            }

            return (response.Status, null);
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

    private void OnOrderPaid(OrderPaymentDetails details)
    {
        Debug.WriteLine("OnOrderPaid");
        PaymentResult = details;
        State = PurchaseState.Paid;
        _isPaid = true;
        _telemetry.TrackEvent("Purchase.Paid", new Dictionary<string, string>
        {
            ["resourceId"] = _resourceId.ToString(),
            ["skuId"] = SelectedSku?.Id.ToString() ?? string.Empty
        });
    }
}

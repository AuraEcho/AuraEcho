using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Strings;
using AuraEcho.Enums;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class PurchaseViewModel : BindableBase, IRegionDialogAware
{
    private Guid _resourceId;
    private readonly ISkuRepository _skuRepository;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuraToastService _auraToastService;
    private readonly IRemotePluginRepository _remotePluginRepository;
    private readonly ISkuOrderCacheService _skuOrderCacheService;
    private readonly IClock _clock;

    private int _currentOrderTaskId;
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
        _orderStatusTaskToken.Cancel();
        RequestClose?.Invoke(RegionDialogResult.Cancel);
    }

    public DelegateCommand CloseCommand { get; }
    private void Close()
    {
        _orderStatusTaskToken.Cancel();
        RequestClose?.Invoke(RegionDialogResult.Close);
    }
    public DelegateCommand OpenSubscriptionTermsCommand { get; }

    public event Action<RegionDialogResult> RequestClose;

    public PurchaseViewModel(
        IClock clock,
        IEventAggregator eventAggregator,
        ISkuRepository skuRepository,
        IOrderRepository orderRepository,
        IAuraToastService auraToastService,
        ILicenseRepository licenseRepository,
        IRemotePluginRepository remotePluginRepository,
        ISkuOrderCacheService skuOrderCacheService)
    {
        _clock = clock;
        _eventAggregator = eventAggregator;
        _skuRepository = skuRepository;
        _orderRepository = orderRepository;
        _auraToastService = auraToastService;
        _licenseRepository = licenseRepository;
        _remotePluginRepository = remotePluginRepository;
        _skuOrderCacheService = skuOrderCacheService;

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(Cancel);
        CloseCommand = new DelegateCommand(Close);
        SelectSkuCommand = new DelegateCommand<Sku>(SelectSku);
        RefreshOrderCommand = new DelegateCommand(RefreshOrderAsync);
        OpenSubscriptionTermsCommand = new DelegateCommand(OpenSubscriptionTerms);

        _eventAggregator.GetEvent<OrderPaidEvent>().Subscribe(OnOrderPaid);
    }

    public async void OnDialogOpened(NavigationParameters parameters)
    {
        _resourceId = parameters.GetValue<Guid>("ResourceId");
        await InitializeAsync(_resourceId);
    }

    private async Task InitializeAsync(Guid resourceId)
    {
        try
        {
            State = PurchaseState.Loading;

            var minTimeTask = Task.Delay(TimeSpan.FromSeconds(.5));
            
            var tPlugin = _remotePluginRepository.GetPluginByIdAsync(resourceId);
            var tSkus = _skuRepository.GetResourceSkusAsync(resourceId);
            var tLicense = GetLicenseInfo(resourceId);
            await Task.WhenAll(tPlugin, tSkus, tLicense);

            CurrentPlugin = tPlugin.Result;
            CurrentPluginLicense = tLicense.Result;
            Skus = new ObservableCollection<Sku>(tSkus.Result.Where(s => s.IsActive));

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

        var task = _skuOrderCacheService.GetOrFetchSkuOrderAsync(
            _resourceId,
            SelectedSku.Id,
            PaymentChannel,
            async (skuId, channel) =>
                await _orderRepository.CreateOrderAsync(new CreateOrderRequest
                {
                    SkuId = SelectedSku.Id,
                    Channel = PaymentChannel
                }));

        int taskId = task.Id;
        _currentOrderTaskId = taskId;
        await Task.WhenAll(task, Task.Delay(TimeSpan.FromSeconds(0.3)));

        if (_currentOrderTaskId != taskId) return false;

        var result = task.Result;
        if (result?.Status == ResultStatus.Success && result.Data is not null)
        {
            QRCode = result.Data.QRCode;
            State = PurchaseState.Ready;
            return true;
        }

        State = PurchaseState.OrderFailed;
        _auraToastService.Show(
            result?.Message ?? Labels.Purchase_QRCodeGenerateFailed,
            ToastLevel.Error);
        return false;
    }


    private async Task<ResourceLicense> GetLicenseInfo(Guid pluginId)
    {
        var license = await _licenseRepository.GetResourceLicenseAsync(pluginId);
        if (!license.IsValid || license.ExpiredAt < _clock.UtcNow)
            return null;
        return license;
    }

    private void OpenSubscriptionTerms()
    {
        string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        string path = Path.Combine(folder, "Assets/PDF/SubscriptionTerms.pdf");
        Task.Run(() => Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = path
        }));
    }

    private void OnOrderPaid(OrderPaymentDetails details)
    {
        Debug.WriteLine("OnOrderPaid");
        PaymentResult = details;
        State = PurchaseState.Paid;
    }
}

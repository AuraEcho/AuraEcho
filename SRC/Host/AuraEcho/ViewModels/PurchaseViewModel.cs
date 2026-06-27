using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Strings;
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

public class PurchaseViewModel : BindableBase, INavigationAware, IRegionDialogAware
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
    private Guid? _newestOrderId;
    private int _currentOrderCreatingTaskId;
    private readonly CancellationTokenSource _orderStatusTaskToken = new();

    public ResourceLicense CurrentPluginLicense
    {
        get;
        set => SetProperty(ref field, value);
    }

    public RemotePlugin CurrentPlugin
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
            bool isUpdated = SetProperty(ref field, value);

            if (!isUpdated) return;
            if (value is null) return;

            SubmitOrder();
        }
    }

    public PaymentChannel PaymentChannel
    {
        get;
        set
        {
            bool isUpdated = SetProperty(ref field, value);
            if (!isUpdated) return;
            if (SelectedSku is null) return;

            SubmitOrder();
        }
    } = PaymentChannel.Wxpay;

    public string QRCode
    {
        get;
        set => SetProperty(ref field, value);
    } = "QRPlaceHolderQRPlaceHolderQRPlaceHolder";

    public bool QRCodeIsValid
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsOrderCreating
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsPaid
    {
        get;
        set => SetProperty(ref field, value);
    }

    public OrderPaymentDetails OrderPaymentDetails
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand<Sku> SelectSkuCommand { get; }
    private void SelectSku(Sku sku)
    {
        SelectedSku = sku;
    }

    public DelegateCommand SubmitOrderCommand { get; }
    private async void SubmitOrder()
    {
        if (SelectedSku is null) return;

        IsOrderCreating = true;

        Task<ResponseResult<CreateOrderResponse>?> createOrderTask =
            _skuOrderCacheService.GetOrFetchSkuOrderAsync(
                SelectedSku.Id,
                PaymentChannel,
                async (skuId, paymentChannel) =>
                    await _orderRepository.CreateOrderAsync(new CreateOrderRequest
                    {
                        SkuId = SelectedSku.Id,
                        Channel = PaymentChannel
                    }));
        _currentOrderCreatingTaskId = createOrderTask.Id;
        await Task.WhenAll([createOrderTask, Task.Delay(TimeSpan.FromSeconds(0.3))]);

        if (_currentOrderCreatingTaskId != createOrderTask.Id) return;

        ResponseResult<CreateOrderResponse>? result = createOrderTask.Result;

        if (result is null)
        {
            QRCodeIsValid = false;
            IsOrderCreating = false;
            _auraToastService.Show(Labels.Purchase_QRCodeGenerateFailed, ToastLevel.Error);
            return;
        }

        if (result.Status != ResultStatus.Success)
        {
            QRCodeIsValid = false;
            IsOrderCreating = false;
            _auraToastService.Show(result.Message, ToastLevel.Error);
            return;
        }

        _newestOrderId = result.Data?.OrderId;
        QRCode = result.Data!.QRCode;
        QRCodeIsValid = true;
        IsOrderCreating = false;
    }

    public DelegateCommand RefreshPayQRCodeCommand { get; }
    private void RefershQRCode()
    {
        _skuOrderCacheService.InvalidateCache(SelectedSku.Id, PaymentChannel);
        SubmitOrder();
    }

    public DelegateCommand OpenSubscriptionTermsCommand { get; }
    private void OpenSubscriptionTerms()
    {
        string currentFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        string filePath = Path.Combine(currentFolderPath, "Assets/PDF/SubscriptionTerms.pdf");

        Task.Run(() =>
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = filePath
            }));
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
        _remotePluginRepository = remotePluginRepository;
        _skuOrderCacheService = skuOrderCacheService;
        _licenseRepository = licenseRepository;

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(Cancel);
        CloseCommand = new DelegateCommand(Close);
        SubmitOrderCommand = new DelegateCommand(SubmitOrder);
        SelectSkuCommand = new DelegateCommand<Sku>(SelectSku);
        RefreshPayQRCodeCommand = new DelegateCommand(RefershQRCode);
        OpenSubscriptionTermsCommand = new DelegateCommand(OpenSubscriptionTerms);

        _eventAggregator.GetEvent<OrderPaidEvent>().Subscribe(OnOrderPaid);
    }

    private void OnOrderPaid(OrderPaymentDetails orderPaymentDetails)
    {
        Debug.WriteLine("OnOrderPaid");
        OrderPaymentDetails = orderPaymentDetails;
        IsPaid = true;
    }

    private async Task LoadPluginInfo(Guid pluginId)
    {
        CurrentPlugin = await _remotePluginRepository.GetPluginByIdAsync(pluginId);
    }

    private async Task LoadSkus(Guid pluginId)
    {
        var skuList = await _skuRepository.GetResourceSkusAsync(pluginId);

        Skus = [.. skuList.Where(s => s.IsActive)];
        SelectedSku = Skus.FirstOrDefault();
    }

    private async Task LoadLicenseInfo(Guid pluginId)
    {
        var licenseInfo = await _licenseRepository.GetResourceLicenseAsync(pluginId);
        if (!licenseInfo.IsValid || licenseInfo.ExpiredAt < _clock.UtcNow)
        {
            CurrentPluginLicense = null;
            return;
        }

        CurrentPluginLicense = licenseInfo;
    }

    public void OnDialogOpened(NavigationParameters parameters)
    {
        _resourceId = parameters.GetValue<Guid>("ResourceId");

        _ = LoadPluginInfo(_resourceId);
        _ = LoadSkus(_resourceId);
        _ = LoadLicenseInfo(_resourceId);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
        => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _orderStatusTaskToken.Cancel();
    }
}

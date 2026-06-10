using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace AuraEcho.ViewModels;

public class PurchaseViewModel : BindableBase, INavigationAware, IRegionDialogAware
{
    private Guid _resourceId;
    private readonly ISkuRepository _skuRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuraToastService _auraToastService;
    private readonly IRemotePluginRepository _remotePluginRepository;
    private readonly ISkuOrderCacheService _skuPayUrlCacheService;
    private Guid? _newestOrderId;
    private readonly CancellationTokenSource _orderStatusTaskToken = new();

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
    }

    public string PayUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool PayUrlIsValid
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsOrderCreating
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int _currentOrderCreatingTaskId;

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
        PayUrlIsValid = true;

        Task<ResponseResult<CreateOrderResponse>?> createOrderTask = 
            _skuPayUrlCacheService.GetOrFetchSkuOrderAsync(
                SelectedSku.Id,
                PaymentChannel,
                async (skuId, paymentChannel) =>
                    await _orderRepository.CreateOrderAsync(new CreateOrderRequest
                    {
                        SkuId = SelectedSku.Id,
                        Channel = PaymentChannel
                    }));
        _currentOrderCreatingTaskId = createOrderTask.Id;
        await Task.WhenAll([ createOrderTask, Task.Delay(TimeSpan.FromSeconds(0.3))]);

        if (_currentOrderCreatingTaskId != createOrderTask.Id) return;

        ResponseResult<CreateOrderResponse>? result = createOrderTask.Result;

        if (result is null)
        {
            PayUrlIsValid = false;
            IsOrderCreating = false;
            _auraToastService.Show("支付二维码生成失败", ToastLevel.Error);
            return;
        }

        if (result.Status != ResultStatus.Success)
        {
            IsOrderCreating = false;
            PayUrlIsValid = false;
            _auraToastService.Show(result.Message, ToastLevel.Error);
            return;
        }

        IsOrderCreating = false;
        PayUrlIsValid = true;
        _newestOrderId = result.Data?.OrderId;
        PayUrl = result.Data?.PayUrl;
    }

    public DelegateCommand RefreshPayUrlCommand { get; }
    private void RefershPayUrl()
    {
        _skuPayUrlCacheService.InvalidateCache(SelectedSku.Id, PaymentChannel);
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

    private async Task CheckOrderStatusAsync()
    {
        while (!_orderStatusTaskToken.IsCancellationRequested)
        {
            await Task.Delay(2000, _orderStatusTaskToken.Token);
            if (!_newestOrderId.HasValue) continue;

            OrderStatus orderStatus = await _orderRepository.GetOrderStatusAsync(_newestOrderId.Value);
            if (orderStatus == OrderStatus.Paid)
            {
                _auraToastService.Show("订单支付成功！", ToastLevel.Success);
                RequestClose?.Invoke(RegionDialogResult.OK);
                break;
            }
        }
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
        ISkuRepository skuRepository,
        IOrderRepository orderRepository,
        IAuraToastService auraToastService,
        IRemotePluginRepository remotePluginRepository,
        ISkuOrderCacheService skuPayUrlCacheService)
    {
        _skuRepository = skuRepository;
        _orderRepository = orderRepository;
        _auraToastService = auraToastService;
        _remotePluginRepository = remotePluginRepository;
        _skuPayUrlCacheService = skuPayUrlCacheService;

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(Cancel);
        CloseCommand = new DelegateCommand(Close);
        SubmitOrderCommand = new DelegateCommand(SubmitOrder);
        SelectSkuCommand = new DelegateCommand<Sku>(SelectSku);
        RefreshPayUrlCommand = new DelegateCommand(RefershPayUrl);
        OpenSubscriptionTermsCommand = new DelegateCommand(OpenSubscriptionTerms);

        _ = CheckOrderStatusAsync();

        PaymentChannel = PaymentChannel.Wxpay;
        PaymentChannel = PaymentChannel.Alipay;
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

    public void OnDialogOpened(NavigationParameters parameters)
    {
        _resourceId = parameters.GetValue<Guid>("ResourceId");

        _ = LoadPluginInfo(_resourceId);
        _ = LoadSkus(_resourceId);
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

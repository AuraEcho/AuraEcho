using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Enums;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Order;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class PurchaseViewModel : BindableBase, IRegionDialogAware
{
    private Guid _resourceId;
    private readonly ISkuRepository _skuRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuraToastService _auraToastService;
    private Guid? _newestOrderId;

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

    public string QRCode
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

        ResponseResult<CreateOrderResponse>? result =
            await _orderRepository.CreateOrderAsync(new CreateOrderRequest
            {
                SkuId = SelectedSku.Id,
                Channel = PaymentChannel,
            });

        if (result is null)
        {
            _auraToastService.Show("创建订单失败", ToastLevel.Error);
            return;
        }

        if (result.Status != Core.Enums.ResultStatus.Success)
        {
            _auraToastService.Show(result.Message, ToastLevel.Error);
            return;
        }

        _newestOrderId = result.Data?.OrderId;
        PayUrl = result.Data?.PayUrl;
        QRCode = result.Data?.QRCode;
    }

    private async Task CheckOrderStatusAsync()
    {
        while (true)
        {
            await Task.Delay(2000);
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
        RequestClose?.Invoke(RegionDialogResult.OK);
    }

    public DelegateCommand CancelCommand { get; }
    private void Cancel()
    {
        RequestClose?.Invoke(RegionDialogResult.Cancel);
    }

    public DelegateCommand CloseCommand { get; }
    private void Close()
    {
        RequestClose?.Invoke(RegionDialogResult.Close);
    }

    public event Action<RegionDialogResult> RequestClose;

    public PurchaseViewModel(ISkuRepository skuRepository, IOrderRepository orderRepository, IAuraToastService auraToastService)
    {
        _skuRepository = skuRepository;
        _orderRepository = orderRepository;
        _auraToastService = auraToastService;

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(Cancel);
        CloseCommand = new DelegateCommand(Close);
        SubmitOrderCommand = new DelegateCommand(SubmitOrder);
        SelectSkuCommand = new DelegateCommand<Sku>(SelectSku);

        _ = CheckOrderStatusAsync();

        PaymentChannel = PaymentChannel.Wxpay;
        PaymentChannel = PaymentChannel.Alipay;
    }

    private async void LoadSkus(Guid pluginId)
    {
        var skuList = await _skuRepository.GetResourceSkusAsync(pluginId);

        Skus = [.. skuList.Where(s => s.IsActive)];
        SelectedSku = Skus.FirstOrDefault();
    }

    public void OnDialogOpened(NavigationParameters parameters)
    {
        _resourceId = parameters.GetValue<Guid>("ResourceId");

        LoadSkus(_resourceId);
    }
}

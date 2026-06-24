using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly HttpHelper _httpHelper;
    public OrderRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }
    public async Task<ResponseResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _httpHelper.PostAsync<ResponseResult<CreateOrderResponse>>(Urls.CreateOrderAsync(), request);

        if (response is null || response.Data is null) return response;

        if (String.IsNullOrEmpty(response.Data.QRCode))
        {
            // TODO: Nullable OrderId
            response.Data.OrderId = Guid.Empty;

            response.Status = ResultStatus.UnknownError;
            response.Message = "创建订单失败";
        }

        return response;
    }

    public async Task<GetOrderByIdResult> GetOrderByIdAsync(Guid orderId)
    {
        var result = await _httpHelper.GetAsync<GetOrderByIdResult>(Urls.GetOrderById(orderId));
        if (result is null) return null;

        result.PayTime = DateTime.SpecifyKind(result.PayTime!.Value, DateTimeKind.Utc).ToLocalTime();
        return result;
    }

    public async Task<OrderStatus> GetOrderStatusAsync(Guid orderId)
    {
        var result = await _httpHelper.GetAsync<OrderStatusResponse>(Urls.GetOrderStatus(orderId));
        if (result is null) return OrderStatus.Pending;

        return result.OrderStatus;
    }
}

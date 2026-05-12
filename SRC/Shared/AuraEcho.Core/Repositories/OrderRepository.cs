using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
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
        return response;
    }

    public async Task<OrderStatus> GetOrderStatusAsync(Guid orderId)
    {
        var result = await _httpHelper.GetAsync<OrderStatusResponse>(Urls.GetOrderStatus(orderId));
        if (result is null) return OrderStatus.Pending;

        return result.OrderStatus;
    }
}

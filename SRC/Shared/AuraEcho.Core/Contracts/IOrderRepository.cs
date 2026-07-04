using AuraEcho.ClientApi.V1.Common;
using AuraEcho.ClientApi.V1.Order;
using AuraEcho.ClientApi.V1.Plugin;

namespace AuraEcho.Core.Contracts;

public interface IOrderRepository
{
    Task<ResponseResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(Guid orderId);
    Task<GetOrderByIdResult> GetOrderByIdAsync(Guid orderId);
}

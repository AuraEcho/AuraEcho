using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using AuraEcho.Api.Models.V1.Plugin;

namespace AuraEcho.Core.Contracts;

public interface IOrderRepository
{
    Task<ResponseResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(Guid orderId);
    Task<GetOrderByIdResult> GetOrderByIdAsync(Guid orderId);
}

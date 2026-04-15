using AuraEcho.Core.Enums;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Order;

namespace AuraEcho.Core.Contracts;

public interface IOrderRepository
{
    Task<ResponseResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(Guid orderId);
}

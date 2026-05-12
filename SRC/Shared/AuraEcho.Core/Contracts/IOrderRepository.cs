using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;

namespace AuraEcho.Core.Contracts;

public interface IOrderRepository
{
    Task<ResponseResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(Guid orderId);
}

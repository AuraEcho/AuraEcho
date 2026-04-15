using AuraEcho.Core.Enums;

namespace AuraEcho.Core.Models.Api;

public class OrderStatusResponse
{
    public Guid OrderId { get; set; }
    public OrderStatus OrderStatus { get; set; }
}

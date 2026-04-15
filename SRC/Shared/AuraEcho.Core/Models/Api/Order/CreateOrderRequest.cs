namespace AuraEcho.Core.Models.Api.Order;

public class CreateOrderRequest
{
    public Guid SkuId { get; set; }
    public PaymentChannel Channel { get; set; }
}

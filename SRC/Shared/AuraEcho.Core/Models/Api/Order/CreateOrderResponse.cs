namespace AuraEcho.Core.Models.Api;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public string PayUrl { get; set; }
    public string QRCode { get; set; }
}

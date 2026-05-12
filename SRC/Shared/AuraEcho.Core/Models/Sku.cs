using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Core.Enums;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models;

public class Sku
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }
    public ResourceType ResourceType { get; set; }

    public LicenseType Type { get; set; }
    public decimal SalePrice { get; set; }
    public decimal OriginalPrice { get; set; }

    public bool IsActive { get; set; }
}

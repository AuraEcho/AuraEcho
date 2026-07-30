using AuraEcho.Cloud.V1.Models.Common;

namespace AuraEcho.Core.Models;

public class Sku
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// 所属订阅等级 Id
    /// </summary>
    public Guid LicenseTierId { get; set; }

    /// <summary>
    /// 等级序数
    /// </summary>
    public int TierLevel { get; set; }

    /// <summary>
    /// 等级名称
    /// </summary>
    public string TierName { get; set; } = string.Empty;

    /// <summary>
    /// 订阅时长（月）
    /// </summary>
    public int DurationMonths { get; set; }

    public decimal SalePrice { get; set; }
    public decimal OriginalPrice { get; set; }

    public bool IsActive { get; set; }
    public int Ordinal { get; set; }
}

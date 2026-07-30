using AuraEcho.Core.Models;
using System.Collections.ObjectModel;

namespace AuraEcho.Models;

/// <summary>
/// SKU 等级分组
/// </summary>
public class SkuTierGroup
{
    /// <summary>
    /// 等级名称
    /// </summary>
    public string TierName { get; set; } = string.Empty;

    /// <summary>
    /// 等级序数
    /// </summary>
    public int TierLevel { get; set; }

    /// <summary>
    /// SKU 列表
    /// </summary>
    public ObservableCollection<Sku> Skus { get; set; } = [];

    /// <summary>
    /// 是否可购买
    /// </summary>
    public bool IsPurchasable { get; set; } = true;

    /// <summary>
    /// 不可购买的原因
    /// </summary>
    public string? LockReason { get; set; }
}

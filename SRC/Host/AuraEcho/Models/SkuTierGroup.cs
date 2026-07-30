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
}

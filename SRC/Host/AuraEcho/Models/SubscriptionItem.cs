using AuraEcho.Strings;
using System;
using System.Globalization;

namespace AuraEcho.Models;

/// <summary>
/// 订阅列表项
/// </summary>
public class SubscriptionItem
{
    /// <summary>
    /// 资源 ID
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// 插件名称
    /// </summary>
    public string PluginName { get; set; }

    /// <summary>
    /// 插件图标地址
    /// </summary>
    public string PluginIconUrl { get; set; }

    /// <summary>
    /// 当前生效的等级名称
    /// </summary>
    public string TierName { get; set; }

    /// <summary>
    /// 当前生效等级的权益描述
    /// </summary>
    public string TierDescription { get; set; }

    /// <summary>
    /// 到期时间（UTC）
    /// </summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// 判定到期状态的时间基准（UTC）
    /// </summary>
    public DateTime Now { get; set; }

    /// <summary>
    /// 剩余天数
    /// </summary>
    public int DaysRemaining =>
        ExpiredAt is null || IsExpired
            ? 0
            : Math.Max(0, (int)Math.Ceiling((ExpiredAt.Value - Now).TotalDays));

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => ExpiredAt is not null && ExpiredAt.Value <= Now;

    /// <summary>
    /// 是否即将到期
    /// </summary>
    public bool IsExpiringSoon => !IsExpired && DaysRemaining <= 7;

    /// <summary>
    /// 剩余天数展示文本。
    /// </summary>
    public string DaysRemainingText =>
        string.Format(CultureInfo.CurrentCulture, Labels.Subscriptions_DaysRemaining, DaysRemaining);
}

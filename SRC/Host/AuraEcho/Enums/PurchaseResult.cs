namespace AuraEcho.Enums;

/// <summary>
/// 订单处理结果
/// </summary>
public enum PurchaseResult
{
    /// <summary>
    /// 无
    /// </summary>
    None,

    /// <summary>
    /// 已支付并开通
    /// </summary>
    Paid,

    /// <summary>
    /// 免费开通成功
    /// </summary>
    FreeProvisioned,

    /// <summary>
    /// 已支付但开通失败
    /// </summary>
    Refunding
}

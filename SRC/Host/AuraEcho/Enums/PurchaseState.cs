namespace AuraEcho.Enums;

public enum PurchaseState
{
    /// <summary>
    /// 加载中
    /// </summary>
    Loading,
    
    /// <summary>
    /// 内容就绪
    /// </summary>
    Ready,
    
    /// <summary>
    /// 正在创建订单
    /// </summary>
    CreatingOrder,

    /// <summary>
    /// 订单创建失败
    /// </summary>
    OrderFailed,

    /// <summary>
    /// 支付成功
    /// </summary>
    Paid
}

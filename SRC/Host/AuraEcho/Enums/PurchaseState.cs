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
    /// 应付金额为 0，等待用户确认开通
    /// </summary>
    ConfirmPending,

    /// <summary>
    /// 正在确认开通零元订单
    /// </summary>
    Confirming,

    /// <summary>
    /// 支付成功
    /// </summary>
    Paid,

    //TODO: 应该区分开通（0元订单）失败/自动退款完成/自动退款失败
    
    /// <summary>
    /// 已收款但无法开通授权，订单已转入退款
    /// </summary>
    Refunding
}

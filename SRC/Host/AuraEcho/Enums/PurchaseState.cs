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
    /// 订单完成
    /// </summary>
    OrderCompleted
}

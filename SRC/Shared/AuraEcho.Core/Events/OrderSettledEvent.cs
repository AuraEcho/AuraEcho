using AuraEcho.Cloud.V1.Models.Order;
using Prism.Events;

namespace AuraEcho.Core.Events;

/// <summary>
/// 订单处理结果事件
/// </summary>
public class OrderSettledEvent : PubSubEvent<OrderSettlement> { }

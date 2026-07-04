using AuraEcho.ClientApi.V1.Order;
using Prism.Events;

namespace AuraEcho.Core.Events;

public class OrderPaidEvent : PubSubEvent<OrderPaymentDetails> { }

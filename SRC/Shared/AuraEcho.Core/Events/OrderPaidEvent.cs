using AuraEcho.Cloud.V1.Models.Order;
using Prism.Events;

namespace AuraEcho.Core.Events;

public class OrderPaidEvent : PubSubEvent<OrderPaymentDetails> { }

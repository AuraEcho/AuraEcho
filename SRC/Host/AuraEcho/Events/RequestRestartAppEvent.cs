using AuraEcho.Models;
using Prism.Events;

namespace AuraEcho.Events;

public class RequestRestartAppEvent : PubSubEvent<PendingRestartItem>
{
}

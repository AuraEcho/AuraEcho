using Prism.Events;

namespace AuraEcho.Core.Events;

public class NewVersionInstalledEvent : PubSubEvent<Version>
{
}

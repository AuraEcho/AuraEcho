using System;
using Prism.Events;

namespace AuraEcho.Events;

public class PluginCancelUninstallEvent : PubSubEvent<Guid>
{
}

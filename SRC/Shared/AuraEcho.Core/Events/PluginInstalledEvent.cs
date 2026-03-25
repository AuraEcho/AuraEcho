using AuraEcho.Core.Models;
using Prism.Events;

namespace AuraEcho.Core.Events;

public class PluginInstalledEvent : PubSubEvent<UserPluginModel>
{
}

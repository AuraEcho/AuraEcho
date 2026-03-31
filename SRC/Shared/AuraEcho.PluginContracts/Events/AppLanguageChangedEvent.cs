using AuraEcho.PluginContracts.Models;
using Prism.Events;

namespace AuraEcho.PluginContracts.Events
{
    public class AppLanguageChangedEvent : PubSubEvent<AppLanguage>
    {
    }
}
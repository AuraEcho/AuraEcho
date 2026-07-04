using AuraEcho.PluginContracts.Models;
using Prism.Ioc;
using System.Threading.Tasks;
using System.Windows;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IPlugin
    {
        string EntryViewName { get; }
        ResourceDictionary GetThemeResource(AppTheme theme);
        Task SetupAsync(IContainerProvider containerProvider);
        void RegisterTypes(IContainerRegistry containerProvider);
        void OnInitialized(IContainerProvider containerProvider);
    }
}

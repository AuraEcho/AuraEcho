using System.Threading.Tasks;
using System.Windows;
using AuraEcho.PluginContracts.Models;
using Prism.Ioc;
using Prism.Modularity;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IPlugin : IModule
    {
        ResourceDictionary GetThemeResource(AppTheme theme);
        AppSettingsItem GetSettings();
        Task SetupAsync(IContainerProvider containerProvider);
    }
}

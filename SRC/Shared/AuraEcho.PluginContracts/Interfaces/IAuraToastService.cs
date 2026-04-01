using AuraEcho.PluginContracts.Models;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IAuraToastService
    {
        void Show(string message, ToastLevel level = ToastLevel.Info, double duration = 3.0);
    }
}

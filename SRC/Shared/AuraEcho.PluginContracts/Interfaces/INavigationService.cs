using Prism.Regions;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface INavigationService
    {
        void RequestNavigate(string regionName, string target, NavigationParameters navigationParameters = null, bool canBack = true);
        bool CanGoBack { get; }
        void GoBack();
        void Reset();
    }
}

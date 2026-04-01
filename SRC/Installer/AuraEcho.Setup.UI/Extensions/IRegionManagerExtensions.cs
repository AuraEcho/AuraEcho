using AuraEcho.Setup.UI;
using Prism.Regions;

namespace AuraEcho.Setup.UI.Extensions
{
    public static class IRegionManagerExtensions
    {
        public static void RequestNavigateOnUIThread(this IRegionManager @this, string regionName, string viewName)
            => App.Current.Dispatcher.Invoke(() => @this.RequestNavigate(regionName, viewName));

        public static void RequestNavigateOnUIThread(this IRegionManager @this, string regionName, string viewName, NavigationParameters parameters)
            => App.Current.Dispatcher.Invoke(() => @this.RequestNavigate(regionName, viewName, parameters));
    }
}

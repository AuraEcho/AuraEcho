using AuraEcho.Core.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Telemetry;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.Core.Services
{
    public class NavigationService(IRegionManager regionManager, ITelemetryService telemetry) : BindableBase, INavigationService
    {
        private readonly IRegionManager _regionManager = regionManager;
        private readonly ITelemetryService _telemetry = telemetry;
        private readonly Stack<NavigationHistoryEntry> _stack = new();

        public void RequestNavigate(string regionName, string target, NavigationParameters? navigationParameters = null, bool canBack = true)
        {
            var entry = new NavigationHistoryEntry(regionName, target, navigationParameters);
            if (_stack.FirstOrDefault() != entry && canBack)
                _stack.Push(entry);

            _regionManager.RequestNavigate(regionName, target, navigationParameters);
            RaisePropertyChanged(nameof(CanGoBack));

            _telemetry.TrackPageView(target);
        }

        public bool CanGoBack => _stack.Count > 1;

        public void GoBack()
        {
            if (_stack.Count == 0)
                return;

            var topEntry = _stack.Pop();
            var entry = _stack.Peek();

            if (topEntry.RegionName != entry.RegionName)
            {
                var region = _regionManager.Regions[topEntry.RegionName];
                region.RemoveAll();
            }

            _regionManager.RequestNavigate(entry.RegionName, entry.ViewName, entry.Parameters);
            RaisePropertyChanged(nameof(CanGoBack));

            _telemetry.TrackPageView(entry.ViewName);
        }

        public void Reset()
        {
            _stack.Clear();
            RaisePropertyChanged(nameof(CanGoBack));
        }
    }

}

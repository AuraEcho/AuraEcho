using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism;
using Prism.Ioc;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace AuraEcho.UIToolkit.RegionDialog
{
    public class RegionDialogService : IRegionDialogService
    {
        private readonly IRegionManager _regionManager;
        private readonly IContainerProvider _container;

        public RegionDialogService(IRegionManager regionManager, IContainerProvider container)
        {
            _regionManager = regionManager;
            _container = container;
        }

        public Task<RegionDialogResult> ShowDialogAsync(string regionName, string target, NavigationParameters parameters)
        {
            var tcs = new TaskCompletionSource<RegionDialogResult>();
            var dialog = _container.Resolve<object>(target);

            if (!_regionManager.Regions.ContainsRegionWithName(regionName))
                throw new InvalidOperationException($"{regionName} not found in Shell.");
            var region = _regionManager.Regions[regionName];

            if (!(dialog is FrameworkElement fe))
                throw new InvalidOperationException($"{target} must be a FrameworkElement.");

            if (!(fe.DataContext is IRegionDialogAware dialogAware))
                throw new InvalidOperationException($"{target} must have a DataContext that implements IRegionDialogAware.");

            dialogAware.RequestClose += FinalizeResult;
            region.Add(dialog, null, true);
            region.Activate(dialog);
            dialogAware.OnDialogOpened(parameters);

            _regionManager.Regions[regionName].NavigationService.Navigated -= OnRegionNavigated;
            _regionManager.Regions[regionName].NavigationService.Navigated += OnRegionNavigated;
            region.Views.CollectionChanged -= OnViewsCollectionChanged;
            region.Views.CollectionChanged += OnViewsCollectionChanged;

            return tcs.Task;

            void FinalizeResult(RegionDialogResult result)
            {
                _regionManager.Regions[regionName].NavigationService.Navigated -= OnRegionNavigated;
                region.Views.CollectionChanged -= OnViewsCollectionChanged;

                if (region.Views.Contains(dialog))
                    region.Remove(dialog);

                tcs.TrySetResult(result);
            }

            // Remove
            void OnViewsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (region.Views.Contains(dialog)) return;
                if (tcs.Task.IsCompleted) return;

                FinalizeResult(RegionDialogResult.Cancel);
            }

            // Navigate
            void OnRegionNavigated(object sender, RegionNavigationEventArgs e)
            {
                if (e.Uri.ToString() != target && !tcs.Task.IsCompleted)
                {
                    FinalizeResult(RegionDialogResult.Cancel);
                }
            }
        }
    }
}
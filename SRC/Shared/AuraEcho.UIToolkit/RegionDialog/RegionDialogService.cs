using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Threading.Tasks;
using System.Windows;

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

        public Task<RegionDialogResult> ShowDialogAsync(string regionName, string target, RegionDialogParameter parameter)
        {
            var tcs = new TaskCompletionSource<RegionDialogResult>();
            var dialog = _container.Resolve<object>(target);

            if (!_regionManager.Regions.ContainsRegionWithName(regionName))
                throw new InvalidOperationException($"{regionName} not found in Shell.");
            var region = _regionManager.Regions[regionName];
            region.Add(dialog, null, true);
            region.Activate(dialog);

            var aware = dialog as IRegionDialogAware ?? (dialog as FrameworkElement)?.DataContext as IRegionDialogAware;
            aware?.OnDialogOpened(parameter);

            var host = dialog as IRegionDialogAware ?? (dialog as FrameworkElement)?.DataContext as IRegionDialogAware;
            if (host != null)
            {
                host.RequestClose += CloseDialog;
            }
            return tcs.Task;

            void CloseDialog(RegionDialogResult result)
            {
                region.Remove(dialog);
                tcs.SetResult(result);
            }
        }
    }
}
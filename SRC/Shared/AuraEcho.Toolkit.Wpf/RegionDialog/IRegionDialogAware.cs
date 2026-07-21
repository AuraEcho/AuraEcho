using System;
using AuraEcho.PluginContracts.Models;
using Prism.Regions;

namespace AuraEcho.Toolkit.Wpf.RegionDialog
{
    public interface IRegionDialogAware
    {
        event Action<RegionDialogResult> RequestClose;

        void OnDialogOpened(NavigationParameters parameters);
    }
}

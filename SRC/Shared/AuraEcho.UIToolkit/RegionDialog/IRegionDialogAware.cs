using System;
using AuraEcho.PluginContracts.Models;
using Prism.Regions;

namespace AuraEcho.UIToolkit.RegionDialog
{
    public interface IRegionDialogAware
    {
        event Action<RegionDialogResult> RequestClose;

        void OnDialogOpened(NavigationParameters parameters);
    }
}

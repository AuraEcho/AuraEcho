using System;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.UIToolkit.RegionDialog
{
    public interface IRegionDialogAware
    {
        event Action<RegionDialogResult> RequestClose;

        void OnDialogOpened(RegionDialogParameter parameters);
    }
}

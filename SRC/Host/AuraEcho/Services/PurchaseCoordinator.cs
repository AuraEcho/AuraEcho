using System;
using System.Threading.Tasks;
using AuraEcho.Constants;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Regions;

namespace AuraEcho.Services;

public class PurchaseCoordinator : IPurchaseCoordinator
{
    private readonly IRegionDialogService _regionDialogService;
    
    public PurchaseCoordinator(IRegionDialogService regionDialogService)
    {
        _regionDialogService = regionDialogService;
    }

    public async Task<bool> RequestPurchaseAsync(Guid resourceId)
    {
        RegionDialogResult dialogResult =
            await _regionDialogService.ShowDialogAsync(
                HostRegionNames.DialogRegion,
                ViewNames.Purchase,
                new NavigationParameters
                {
                    { "ResourceId", resourceId }
                });

        return dialogResult == RegionDialogResult.OK;
    }
}

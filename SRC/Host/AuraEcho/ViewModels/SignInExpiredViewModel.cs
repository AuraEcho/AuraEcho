using AuraEcho.Constants;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class SignInExpiredViewModel : BindableBase
{
    private readonly INavigationService _navigationService;
    private readonly IRegionManager _regionManager;

    public DelegateCommand SignInCommand { get; }
    private void SignIn()
    {
        _regionManager.Regions[HostRegionNames.DialogRegion].RemoveAll();
        _regionManager.Regions[HostRegionNames.MainRegion].RemoveAll();
        _navigationService.Reset();
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SignIn, canBack: false);
    }

    public SignInExpiredViewModel(INavigationService navigationService, IRegionManager regionManager)
    {
        _navigationService = navigationService;
        _regionManager = regionManager;

        SignInCommand = new DelegateCommand(SignIn);
    }
}

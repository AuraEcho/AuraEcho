using AuraEcho.Constants;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class PasswordResetCompletedViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly INavigationService _navigationService;
    public DelegateCommand BackToSignInCommand { get; }
    private void BackToSignIn()
    {
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SignIn, canBack: false);
    }

    public PasswordResetCompletedViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        BackToSignInCommand = new DelegateCommand(BackToSignIn);
    }

    public bool KeepAlive => false;
}

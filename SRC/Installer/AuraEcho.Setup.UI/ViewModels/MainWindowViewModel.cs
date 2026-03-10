using AuraEcho.Setup.UI.WixToolset;
using AuraEcho.Setup.UI.Constants;
using AuraEcho.Setup.UI.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;
using WixToolset.BootstrapperApplicationApi;

namespace AuraEcho.Setup.UI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly AuraEchoBootstrapper _ba;

    private void DetectCompleted(object? sender, EventArgs e)
    {
        var targetView = _ba.Downgrade switch
        {
            true => InstallerViewNames.DowngradeDetected,
            false when _ba.Command.Action == LaunchAction.Uninstall => InstallerViewNames.UninstallPreparation,
            _ => InstallerViewNames.InstallPreparation
        };

        _regionManager.RequestNavigateOnUIThread(InstallerRegionNames.MainRegion, targetView);
    }

    public Version Version => _ba.Version;

    public DelegateCommand ExitCommand { get; }
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    public DelegateCommand CloseSplashScreenCommand { get; }
    private void CloseSplashScreen() => _ba.Engine.CloseSplashScreen();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="model"></param>
    public MainWindowViewModel(AuraEchoBootstrapper ba, IRegionManager regionManager)
    {
        _ba = ba;
        _regionManager = regionManager;
        ExitCommand = new DelegateCommand(Exit);
        CloseSplashScreenCommand = new DelegateCommand(CloseSplashScreen);

        _ba.OnActionRequested += DetectCompleted;
        _ba.Engine.Detect();
    }
}

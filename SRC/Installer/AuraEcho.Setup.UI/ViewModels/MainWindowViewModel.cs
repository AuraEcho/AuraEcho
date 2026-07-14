using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Setup.UI.Constants;
using AuraEcho.Setup.UI.Extensions;
using AuraEcho.Setup.UI.Models;
using AuraEcho.Setup.UI.WixToolset;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Windows;
using WixToolset.BootstrapperApplicationApi;

namespace AuraEcho.Setup.UI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IRegionDialogService _regionDialogService;
        private readonly AuraEchoBootstrapper _ba;

        private void DetectCompleted(object sender, EventArgs e)
        {
            if (_ba.Downgrade)
            {
                _regionManager.RequestNavigateOnUIThread(
                    InstallerRegionNames.MainRegion,
                    InstallerViewNames.InstallFailed,
                    new NavigationParameters
                    {
                        { "Message", "此计算机已存在更高版本的灵光回声。" }
                    });
                return;
            }

            string targetView =
                _ba.Command.Action == LaunchAction.Uninstall
                ? InstallerViewNames.UninstallPreparation
                : InstallerViewNames.InstallPreparation;

            _regionManager.RequestNavigateOnUIThread(
                InstallerRegionNames.MainRegion, 
                targetView);
        }

        public Version Version => _ba.Version;

        public InstallState InstallState => _ba.InstallState;

        public DelegateCommand ExitCommand { get; }
        private async void Exit()
        {
            if (_ba.InstallState == InstallState.Applying)
            {
                var installRegionDialogParameter = new RegionDialogParameter
                {
                    Title = "取消安装",
                    Message = "如果现在取消，安装程序将撤销已更改的操作并清理临时文件。这可能需要几分钟时间。",
                    ConfirmText = "继续安装",
                    CancelText = "停止安装",
                };

                var uninstallRegionDialogParameter = new RegionDialogParameter
                {
                    CancelText = "停止卸载",
                    ConfirmText = "继续卸载",
                    Message = "卸载尚未完成。如果现在退出，AuraEcho 可能无法正常运行。确定要停止卸载吗？",
                    Title = "取消卸载"
                };

                RegionDialogParameter regionDialogParameter = 
                    _ba.Command.Action == LaunchAction.Uninstall 
                    ? uninstallRegionDialogParameter 
                    : installRegionDialogParameter;

                RegionDialogResult dialogResult =
                    await _regionDialogService.ShowDialogAsync(
                        InstallerRegionNames.MessageRegion,
                        HostRegionDialogTypes.ConfirmDialog,
                        new NavigationParameters
                        {
                            { "DialogArgs", regionDialogParameter}
                        });

                if (dialogResult != RegionDialogResult.Cancel)
                    return;

                if (_ba.InstallState == InstallState.Applying)
                    _ba.Cancel();

                return;
            }
            Application.Current.Shutdown();
        }

        public DelegateCommand InitCommand { get; }
        public void Init()
        {
            _ba.Engine.CloseSplashScreen();
            _ba.Engine.Detect();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="model"></param>
        public MainWindowViewModel(AuraEchoBootstrapper ba, IRegionManager regionManager, IRegionDialogService regionDialogService)
        {
            _ba = ba;
            _regionDialogService = regionDialogService;
            _regionManager = regionManager;
            ExitCommand = new DelegateCommand(Exit);
            InitCommand = new DelegateCommand(Init);

            _ba.OnActionRequested += DetectCompleted;
        }
    }
}

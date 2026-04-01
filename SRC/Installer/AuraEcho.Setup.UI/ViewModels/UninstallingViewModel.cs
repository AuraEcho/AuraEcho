using AuraEcho.Setup.UI.WixToolset;
using AuraEcho.Setup.UI.Constants;
using AuraEcho.Setup.UI.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;

namespace AuraEcho.Setup.UI.ViewModels
{
    public class UninstallingViewModel : BindableBase
    {
        private readonly AuraEchoBootstrapper _ba;
        private readonly IRegionManager _regionManager;
        private string message;
        private int _progress;
        private bool _isCreateDesktopFolderShortcut;
        private bool _isRunAtBoot;
        #region Command
        /// <summary>
        /// 执行安装命令
        /// </summary>
        public DelegateCommand UninstallCommand { get; }
        private void Uninstall()
        {
            _ba.Uninstall();
        }
        public DelegateCommand CancelCommand { get; }
        private void Cancel()
        {
            _ba.Cancel();
        }
        #endregion

        #region 属性

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }
        /// <summary>
        /// 安装总进度
        /// </summary>
        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }


        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="model"></param>
        public UninstallingViewModel(AuraEchoBootstrapper ba, IRegionManager regionManager)
        {
            _ba = ba;
            _regionManager = regionManager;

            UninstallCommand = new DelegateCommand(Uninstall);
            CancelCommand = new DelegateCommand(Cancel);
            SubscriptionInstallEvents();
        }

        #endregion



        #region 方法

        private void SubscriptionInstallEvents()
        {
            _ba.OnActionCompleted += UninstallCompleted;
            _ba.ExecuteMessage += ExecuteMsiMessage;
            _ba.ProgressChanged += UpdateProgress;
        }

        private void ExecuteMsiMessage(object sender, string e)
        {
            Message = e;
        }

        private void UpdateProgress(object sender, int e)
        {
            Progress = e;
        }

        private void UninstallCompleted(object sender, int e)
        {
            if (_ba.CancelRequested)
            {
                _regionManager.RequestNavigateOnUIThread(
                    InstallerRegionNames.MainRegion, 
                    InstallerViewNames.InstallFailed,
                    new NavigationParameters
                    {
                        { "Message", "卸载过程已取消。您的系统未被修改。" }
                    });
                return;
            }

            if (e != 0)
            {
                _regionManager.RequestNavigateOnUIThread(
                    InstallerRegionNames.MainRegion,
                    InstallerViewNames.InstallFailed,
                    new NavigationParameters
                    {
                        { "Message", $"卸载过程中发生错误，错误代码：{e}。" }
                    });
                return;
            }

            _regionManager.RequestNavigateOnUIThread(InstallerRegionNames.MainRegion, InstallerViewNames.UninstallFinish);
        }

        private void UnsubscriptionInstallEvents()
        {
            _ba.OnActionCompleted -= UninstallCompleted;
            _ba.ProgressChanged -= UpdateProgress;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _isCreateDesktopFolderShortcut = (bool)navigationContext.Parameters["IsCreateDesktopFolderShortcut"];
            _isRunAtBoot = (bool)navigationContext.Parameters["IsRunAtBoot"];
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
            => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            UnsubscriptionInstallEvents();
        }

        #endregion
    }
}

using AuraEcho.Setup.UI.WixToolset;
using AuraEcho.Setup.UI.Constants;
using AuraEcho.Setup.UI.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using WixToolset.BootstrapperApplicationApi;
using System;

namespace AuraEcho.Setup.UI.ViewModels
{
    public class InstallingViewModel : BindableBase, INavigationAware
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
        public DelegateCommand InstallCommand { get; }
        private async void Install()
        {
            _ba.Install();
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
        public InstallingViewModel(AuraEchoBootstrapper ba, IRegionManager regionManager)
        {
            _ba = ba;
            _regionManager = regionManager;

            InstallCommand = new DelegateCommand(Install);
            CancelCommand = new DelegateCommand(Cancel);
            SubscriptionInstallEvents();
        }

        #endregion

        #region 方法

        private void SubscriptionInstallEvents()
        {
            _ba.OnActionCompleted += InstallCompleted;
            _ba.PlanFeature += PlanMsiFeature;
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

        private void InstallCompleted(object sender, int e)
        {
            if (_ba.CancelRequested)
            {
                _regionManager.RequestNavigateOnUIThread(
                    InstallerRegionNames.MainRegion, 
                    InstallerViewNames.InstallFailed,
                    new NavigationParameters
                    {
                        { "Message", "安装过程已取消。您的系统未被修改。" }
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
                        { "Message", $"安装过程中发生错误，错误代码：{e}。" }
                    });
                return;
            }


            _regionManager.RequestNavigateOnUIThread(InstallerRegionNames.MainRegion, InstallerViewNames.InstallFinish);
        }

        private void UnsubscriptionInstallEvents()
        {
            _ba.OnActionCompleted -= InstallCompleted;
            _ba.PlanFeature -= PlanMsiFeature;
            _ba.ExecuteMessage -= ExecuteMsiMessage;
            _ba.ProgressChanged -= UpdateProgress;
        }

        private void PlanMsiFeature(object sender, PlanMsiFeatureEventArgs e)
        {
            if (e.FeatureId == "DesktopShortcut")
            {
                e.State = _isCreateDesktopFolderShortcut ? FeatureState.Local : FeatureState.Absent;
                return;
            }
            if (e.FeatureId == "RunAtBoot")
            {
                e.State = _isRunAtBoot ? FeatureState.Local : FeatureState.Absent;
                return;
            }
            e.State = FeatureState.Local;
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

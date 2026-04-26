using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using AuraEcho.Setup.UI.WixToolset;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Setup.UI.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using AuraEcho.Setup.UI.Constants;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AuraEcho.Setup.UI.ViewModels
{
    public class InstallPreparationViewModel : BindableBase
    {
        private readonly AuraEchoBootstrapper _ba;
        private readonly IRegionManager _regionManager;
        private readonly IRegionDialogService _regionDialogService;
        private bool _isCreateDesktopFolderShortcut;
        private bool _isRunAtBoot;
        #region Command
        /// <summary>
        /// 打开协议声明
        /// </summary>
        public DelegateCommand OpenEULACommand { get; }
        private void OpenEULA()
        {
            string currentFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string filePath = Path.Combine(currentFolderPath, "EULA.pdf");

            Task.Run(() =>
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = filePath
                }));
        }

        public DelegateCommand<string> SetInstallFolderCommand { get; }
        private void SetInstallFolder(string folderPath)
        {
            TargetInstallFolder = folderPath;
        }

        /// <summary>
        /// 执行安装命令
        /// </summary>
        public DelegateCommand InstallCommand { get; }
        private async void Install()
        {
            if (!await StopAppAsync()) return;

            _regionManager.RequestNavigate(
                InstallerRegionNames.MainRegion,
                InstallerViewNames.Installing,
                new NavigationParameters
                {
                    { "IsCreateDesktopFolderShortcut", IsCreateDesktopFolderShortcut },
                    { "IsRunAtBoot", IsRunAtBoot }
                });
        }
        private async Task<bool> StopAppAsync()
        {
            Process[] allProcesses = Process.GetProcessesByName(ProcessNames.HostProcess);

            if (allProcesses.Length <= 0) return true;

            DirectoryInfo installFolder = new DirectoryInfo(GetInstallPath());
            List<Process> runningProcesses =
                allProcesses.Where(p =>
                {
                    string exePath = p.GetExecutablePath();
                    if (String.IsNullOrEmpty(exePath)) return false;

                    DirectoryInfo processDir = new DirectoryInfo(Path.GetDirectoryName(exePath));
                    return String.Equals(
                        processDir.FullName.TrimEnd('\\'),
                        installFolder.FullName.TrimEnd('\\'),
                        StringComparison.OrdinalIgnoreCase);
                }).ToList();

            if (runningProcesses.Count <= 0) return true;

            RegionDialogResult dialogResult =
                await _regionDialogService.ShowDialogAsync(
                    InstallerRegionNames.MessageRegion,
                    HostRegionDialogTypes.ConfirmDialog,
                    new NavigationParameters
                    {
                        { "DialogArgs", new RegionDialogParameter
                        {
                            CancelText = "重试",
                            ConfirmText = "继续",
                            Message = @"AuraEcho 仍在运行，正在等待 AuraEcho 退出，选择 ""继续"" 以退出 AuraEcho 继续安装。",
                            Title = "AuraEcho 仍在运行"
                        }}
                    });

            if (dialogResult == RegionDialogResult.Close) return false;

            if (dialogResult != RegionDialogResult.OK)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                return await StopAppAsync();
            }

            runningProcesses.ForEach(p => p.Kill());
            return true;
        }
        private static string GetInstallPath()
        {
            const string keyPath = @"Software\AuraEcho";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key == null) return null;

                object value = key.GetValue("InstallPath");
                return value?.ToString();
            }
        }
        #endregion

        #region 属性
        /// <summary>
        /// 是否创建桌面快捷方式
        /// </summary>
        public bool IsCreateDesktopFolderShortcut
        {
            get => _isCreateDesktopFolderShortcut;
            set => SetProperty(ref _isCreateDesktopFolderShortcut, value);
        }
        /// <summary>
        /// 开机自启
        /// </summary>
        public bool IsRunAtBoot
        {
            get => _isRunAtBoot;
            set => SetProperty(ref _isRunAtBoot, value);
        }

        public string TargetInstallFolder
        {
            get => _ba.InstallDirectory;
            set
            {
                string targetFolder = value;
                if (Directory.GetFiles(targetFolder).Length != 0 || Directory.GetDirectories(targetFolder).Length != 0)
                {
                    targetFolder = Path.Combine(targetFolder, "AuraEcho");
                }
                _ba.InstallDirectory = targetFolder;
                RaisePropertyChanged(nameof(TargetInstallFolder));
            }
        }
        #endregion 

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="model"></param>
        public InstallPreparationViewModel(
            AuraEchoBootstrapper ba,
            IRegionManager regionManager,
            IRegionDialogService regionDialogService)
        {
            _ba = ba;
            _regionManager = regionManager;
            _regionDialogService = regionDialogService;

            IsCreateDesktopFolderShortcut = true;
            IsRunAtBoot = true;

            InstallCommand = new DelegateCommand(Install);
            OpenEULACommand = new DelegateCommand(OpenEULA);
            SetInstallFolderCommand = new DelegateCommand<string>(SetInstallFolder);
        }
        #endregion
    }
}

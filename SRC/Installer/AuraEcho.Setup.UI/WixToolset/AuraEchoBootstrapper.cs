using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using AuraEcho.Setup.UI.Constants;
using WixToolset.BootstrapperApplicationApi;
using ErrorEventArgs = WixToolset.BootstrapperApplicationApi.ErrorEventArgs;
using Microsoft.Toolkit.Uwp.Notifications;
using AuraEcho.Setup.UI.Models;
using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AuraEcho.Setup.UI.WixToolset
{
    public sealed class AuraEchoBootstrapper : BootstrapperApplication
    {
        private AuraEchoBootstrapper() { }
        public static readonly Mutex InstallerMutex = new Mutex(false, @"Global\17FA29D6-F4BC-4720-A55C-27042D247E35");
        public static AuraEchoBootstrapper Instance { get; } = new AuraEchoBootstrapper();
        public static bool IsLauchAppWhenInstalled { get; set; }

        private const string PIPE_NAME = "AuraEcho_Installer_Pipe";
        private Dispatcher _dispatcher;
        private bool _isAutoPlan;
        private bool _isCurrentBundleRegistered;
        private ExecuteMsiMessageEventArgs _currentAction;
        private ManualResetEventSlim _elevateLock = new ManualResetEventSlim(false);
        public bool Downgrade { get; private set; }
        public IEngine Engine { get; private set; }
        public IBootstrapperCommand Command { get; private set; }
        private readonly object _syncRoot = new object();
        public RelatedBundleInfo RelatedBundle { get; private set; }
        private int _progressPhases;
        private int _cacheProgress;
        private int _executeProgress;

        private WixStringVariable _installDirVar;
        private WixStringVariable _removeLocalDataOnUninstall;
        private WixStringVariable _uninstallerPath;
        private WixBooleanVariable _createShortcutVar;
        private WixBooleanVariable _launchOnStartupVar;
        private WixStringVariable _versionVar;
        private WixStringVariable _bundleElevated;
        private WixStringVariable _bundleFileName;
        private WixStringVariable _appLauncherName;

        public event EventHandler OnActionRequested;
        public event EventHandler OnActionCompleted;
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<PlanMsiFeatureEventArgs> PlanFeature;
        public event EventHandler<string> ExecuteMessage;
        public event EventHandler OnCanceled;

        public InstallState InstallState { get; private set; }

        public string InstallDirectory
        {
            get => _installDirVar.Get();
            set => _installDirVar.Set(value);
        }

        public string RemoveLocalDataOnUninstall
        {
            get => _removeLocalDataOnUninstall.Get();
            set => _removeLocalDataOnUninstall.Set(value);
        }

        public string AppLauncherFullName
            => Path.Combine(InstallDirectory, _appLauncherName.Get());

        public string UninstallerPath
        {
            get => _uninstallerPath.Get();
            set => _uninstallerPath.Set(value);
        }

        public bool CreateDesktopShortcut
        {
            get => _createShortcutVar.Get();
            set => _createShortcutVar.Set(value);
        }

        public bool LaunchOnStartup
        {
            get => _launchOnStartupVar.Get();
            set => _launchOnStartupVar.Set(value);
        }

        public string BundleFileName
        {
            get => _bundleFileName.Get();
            set => _bundleFileName.Set(value);
        }

        public Version Version => Version.Parse(_versionVar.Get());
        public bool IsBundleElevated => _bundleElevated.Get() is "1";
        public bool CancelRequested { get; private set; }
        protected override void OnCreate(CreateEventArgs args)
        {
            base.OnCreate(args);
            Engine = args.Engine;
            Command = args.Command;

            Engine.Log(LogLevel.Standard, $"Command: {Command.Action} | Display: {Command.Display}");

            _installDirVar = new WixStringVariable(Engine, BundleVar.InstallDirectory);
            _removeLocalDataOnUninstall = new WixStringVariable(Engine, BundleVar.RemoveLocalDataOnUninstall);
            _appLauncherName = new WixStringVariable(Engine, BundleVar.AppLauncherName);
            _createShortcutVar = new WixBooleanVariable(Engine, BundleVar.CreateDesktopShortcut);
            _bundleFileName = new WixStringVariable(Engine, BundleVar.BundleFileName);
            _launchOnStartupVar = new WixBooleanVariable(Engine, BundleVar.LaunchOnStartup);
            _uninstallerPath = new WixStringVariable(Engine, BundleVar.UninstallerPath);
            _versionVar = new WixStringVariable(Engine, BundleVar.Version);
            _bundleElevated = new WixStringVariable(Engine, BundleVar.BundleElevated);
            RelatedBundle = new RelatedBundleInfo();
            InitVariables();
        }

        private void InitVariables()
        {
            string installDirRowValue = engine.GetVariableString(_installDirVar.WixVariableName);
            _installDirVar.Set(engine.FormatString(installDirRowValue));

            _createShortcutVar.Set(true);
            _launchOnStartupVar.Set(true);

            var uninstallPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Package Cache",
                    Engine.GetVariableString("WixBundleProviderKey"),
                    BundleFileName);
            _uninstallerPath.Set(uninstallPath);
        }
        public void SendNotification()
        {
            var notification = new ToastContentBuilder().AddText("另一个安装程序已在运行。");
            notification.Show();
        }
        protected override void Run()
        {
            if (Command.ParseCommandLine().UnknownCommandLineArgs.Contains("-debug", StringComparer.OrdinalIgnoreCase))
                Debugger.Launch();

            try
            {
                Engine.Log(LogLevel.Standard, "Running the AuraEcho.InstallerUI.");

                if (Command.Display != Display.Embedded)
                {
                    if (!InstallerMutex.WaitOne(TimeSpan.Zero, true))
                    {
                        SendNotification();
                        Engine.Log(LogLevel.Standard, "Exiting the AuraEcho.InstallerUI.");
                        Engine.Quit(0);
                        return;
                    }

                    StartPipeServer();
                }

                if (!IsBundleElevated)
                {
                    Engine.Elevate(IntPtr.Zero);
                }
                else
                {
                    _elevateLock.Set();
                }

                LaunchApp();
                Engine.Log(LogLevel.Standard, "Exiting the AuraEcho.InstallerUI.");
                Engine.Quit(0);
            }
            catch (AbandonedMutexException) { }
            catch (Exception ex)
            {
                Engine.Log(LogLevel.Error, $"The AuraEcho.InstallerUI is failed: {ex}");
                Engine.Quit(-1);
            }
            finally
            {
                Engine.Log(LogLevel.Standard, "The AuraEcho.InstallerUI has exited.");
            }
        }

        private static void StartPipeServer()
        {
            Task.Run(async () =>
            {
                var pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(
                    new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));

                while (true)
                {
                    using (var server = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        0,
                        0,
                        pipeSecurity))
                    {
                        await server.WaitForConnectionAsync();

                        using (var reader = new StreamReader(server))
                        {
                            string cmd = await reader.ReadLineAsync();

                            if (cmd == NamedPipeMessages.LaunchAppWhenInstalled)
                            {
                                IsLauchAppWhenInstalled = true;
                            }
                        }
                    }
                }
            });
        }

        private string GetRelatedBundleInstallationFolder()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\AuraEcho", false))
            {
                return key?.GetValue("LauncherPath") is string launcherPath
                       ? Path.GetDirectoryName(launcherPath)
                       : String.Empty;
            }
        }
        private Dictionary<string, bool> LoadRelatedFeatureStatus()
        {
            return new Dictionary<string, bool>
            {
                ["DesktopShortcut"] = LoadDesktopShortcutFeatureStatus(),
                ["RunAtBoot"] = CheckRunAtBoot()
            };

            bool CheckRunAtBoot()
            {
                using (RegistryKey itemKeyRoot =
                    Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (itemKeyRoot.GetValue("AuraEcho") is null) return false;
                }

                using (RegistryKey approvedKeyRoot =
                    Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"))
                {
                    if (!(approvedKeyRoot.GetValue("AuraEcho") is byte[] key)) return true;
                    return key[0] % 2 == 0;
                }
            }
            bool LoadDesktopShortcutFeatureStatus()
            {
                const string keyPath = @"Software\AuraEcho";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    return key?.GetValue("DesktopShortcutInstalled") != null;
                }
            }
        }

        private void LaunchApp()
        {
            _elevateLock.Wait();

            if (!IsBundleElevated)
            {
                Engine.Log(LogLevel.Standard, "Elevated failed.");
                return;
            }

            _dispatcher = Dispatcher.CurrentDispatcher;
            if (Command.Display is Display.Passive || Command.Display is Display.Full)
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
                return;
            }

            _isAutoPlan = true;

            Engine.Detect();
            Dispatcher.Run();
        }

        public void Install()
        {
            if (_isCurrentBundleRegistered || RelatedBundle.Version == Version)
            {
                Plan(LaunchAction.Repair);
                return;
            }

            Plan(LaunchAction.Install);
        }

        public void Uninstall() => Plan(LaunchAction.Uninstall);

        public void Cancel()
        {
            CancelRequested = true;
            ExecuteMessage?.Invoke(this, "正在取消...");
        }

        /// <inheritdoc/>
        protected override void OnApplyBegin(ApplyBeginEventArgs args)
        {
            base.OnApplyBegin(args);
            InstallState = InstallState.Applying;
            _progressPhases = args.PhaseCount;
        }

        public void LaunchExecutedExe(string command, string args)
        {
            Process.Start(command, args);
        }

        /// <inheritdoc/>
        protected override void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            base.OnApplyComplete(args);
            InstallState = InstallState.Completed;
            OnActionCompleted?.Invoke(this, EventArgs.Empty);

            if (!_isAutoPlan) return;

            if (IsLauchAppWhenInstalled)
                LaunchExecutedExe(AppLauncherFullName, "-hide");

            _dispatcher.InvokeShutdown();
            return;
        }

        /// <inheritdoc/>
        protected override void OnCacheAcquireProgress(CacheAcquireProgressEventArgs args)
        {
            base.OnCacheAcquireProgress(args);
            ExecuteMessage?.Invoke(this, "正在准备安装文件...");
            lock (_syncRoot)
            {
                _cacheProgress = args.OverallPercentage;
                ReportProgress();
                args.Cancel = CancelRequested;
            }
        }

        protected override void OnPlanRollbackBoundary(PlanRollbackBoundaryEventArgs args)
        {
            base.OnPlanRollbackBoundary(args);
            Engine.Log(LogLevel.Error, $"OnPlanRollbackBoundary 已执行");
        }

        protected override void OnRollbackMsiTransactionBegin(RollbackMsiTransactionBeginEventArgs args)
        {
            base.OnRollbackMsiTransactionBegin(args);
            Engine.Log(LogLevel.Error, $"OnRollbackMsiTransactionBegin 已执行");
        }

        protected override void OnRollbackMsiTransactionComplete(RollbackMsiTransactionCompleteEventArgs args)
        {
            base.OnRollbackMsiTransactionComplete(args);
            Engine.Log(LogLevel.Error, $"OnRollbackMsiTransactionComplete 已执行");
        }

        /// <inheritdoc/>
        protected override void OnCacheComplete(CacheCompleteEventArgs args)
        {
            base.OnCacheComplete(args);
            lock (_syncRoot)
            {
                _cacheProgress = 100;
                ReportProgress();
            }
        }

        /// <inheritdoc/>
        protected override void OnCacheContainerOrPayloadVerifyProgress(CacheContainerOrPayloadVerifyProgressEventArgs args)
        {
            base.OnCacheContainerOrPayloadVerifyProgress(args);

            ExecuteMessage?.Invoke(this, "正在验证组件完整性...");
            lock (_syncRoot)
            {
                _cacheProgress = args.OverallPercentage;
                ReportProgress();
                args.Cancel = CancelRequested;
            }
        }

        /// <inheritdoc/>
        protected override void OnCachePayloadExtractProgress(CachePayloadExtractProgressEventArgs args)
        {
            base.OnCachePayloadExtractProgress(args);
            lock (_syncRoot)
            {
                _cacheProgress = args.OverallPercentage;
                ReportProgress();
                args.Cancel = CancelRequested;
            }
        }

        /// <inheritdoc/>
        protected override void OnCacheVerifyProgress(CacheVerifyProgressEventArgs args)
        {
            base.OnCacheVerifyProgress(args);
            ExecuteMessage?.Invoke(this, "正在验证组件完整性...");
            lock (_syncRoot)
            {
                _cacheProgress = args.OverallPercentage;
                ReportProgress();
                args.Cancel = CancelRequested;
            }
        }

        /// <inheritdoc/>
        protected override void OnDetectBegin(DetectBeginEventArgs args)
        {
            base.OnDetectBegin(args);

            _isCurrentBundleRegistered = args.RegistrationType != RegistrationType.None;
        }

        /// <inheritdoc/>
        protected override void OnDetectComplete(DetectCompleteEventArgs args)
        {
            base.OnDetectComplete(args);
            if (Command.Action != LaunchAction.Uninstall)
            {
                Downgrade = RelatedBundle.Version > Version;
            }

            OnActionRequested?.Invoke(this, EventArgs.Empty);

            if (!_isAutoPlan) return;

            if (Command.Action == LaunchAction.Install)
            {
                Install();
                return;
            }

            if (Command.Action == LaunchAction.Uninstall)
            {
                Uninstall();
                return;
            }

            Engine.Log(LogLevel.Error, $"意外的自动计划：{Command.Action}");
        }

        /// <inheritdoc/>
        protected override void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            base.OnDetectPackageComplete(args);
        }

        protected override void OnPlanRelatedBundle(PlanRelatedBundleEventArgs args)
        {
            base.OnPlanRelatedBundle(args);

            //if (ExistsBundles.Any(i => args.BundleCode == i.Code))
            //    args.State = RequestState.Absent;
        }

        /// <inheritdoc/>
        protected override void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args)
        {
            base.OnDetectRelatedBundle(args);

            RelatedBundle.Version = Version.Parse(args.Version);
            RelatedBundle.FeatureStatus = LoadRelatedFeatureStatus();
            RelatedBundle.InstallationFolder = GetRelatedBundleInstallationFolder();

            if (!_isAutoPlan) return;

            InstallDirectory = RelatedBundle.InstallationFolder;
        }

        /// <inheritdoc/>
        protected override void OnDetectUpdate(DetectUpdateEventArgs args)
        {
            base.OnDetectUpdate(args);

            // TODO: 
        }

        /// <inheritdoc/>
        protected override void OnDetectUpdateBegin(DetectUpdateBeginEventArgs args)
        {
            base.OnDetectUpdateBegin(args);

            // TODO: 
        }

        /// <inheritdoc/>
        protected override void OnDetectUpdateComplete(DetectUpdateCompleteEventArgs args)
        {
            base.OnDetectUpdateComplete(args);

            // TODO: 
        }

        /// <inheritdoc/>
        protected override void OnElevateComplete(ElevateCompleteEventArgs args)
        {
            base.OnElevateComplete(args);

            _elevateLock.Set();
        }

        /// <inheritdoc/>
        protected override void OnError(ErrorEventArgs args)
        {
            base.OnError(args);
            Engine.Log(LogLevel.Error, $"发生错误: {args.ErrorMessage}");
            ExecuteMessage?.Invoke(this, $"Error: {args.ErrorMessage}");
        }

        protected override void OnExecutePackageComplete(ExecutePackageCompleteEventArgs args)
        {
            Engine.Log(LogLevel.Standard, $"Package {args.PackageId} execution completed with status {args.Status}.");
        }

        /// <inheritdoc/>
        protected override void OnExecuteFilesInUse(ExecuteFilesInUseEventArgs args)
        {
            base.OnExecuteFilesInUse(args);
            Engine.Log(LogLevel.Standard, "FilesInUse detected, cancelling operation.");
        }

        /// <inheritdoc/>
        protected override void OnExecuteMsiMessage(ExecuteMsiMessageEventArgs args)
        {
            base.OnExecuteMsiMessage(args);

            args.Result = CancelRequested ? Result.Cancel : Result.Ok;

            if (args.Result == Result.Cancel)
                return;

            string formattedMessage = FormatMessage(args);
            if (String.IsNullOrWhiteSpace(formattedMessage)) return;

            ExecuteMessage?.Invoke(this, formattedMessage);
        }

        private string FormatMessage(ExecuteMsiMessageEventArgs args)
        {
            if (args.MessageType == InstallMessage.ActionStart && args.Data.Count > 1)
            {
                _currentAction = args;

                string actionMessage = args.Data[1].Split(' ').First().TrimEnd('：', ':');
                return String.IsNullOrWhiteSpace(actionMessage) ? actionMessage : $"{actionMessage}...";
            }

            if (args.MessageType == InstallMessage.ActionData)
            {
                var dataIndexRegex = new Regex(@"\[(\d+)\]");
                if (!dataIndexRegex.IsMatch(_currentAction.Data[1])) return String.Empty;

                if (!args.Message.StartsWith("1:")) return String.Empty;

                string result = dataIndexRegex.Replace(_currentAction.Data[1], m =>
                {
                    if (int.TryParse(m.Groups[1].Value, out int index))
                    {
                        int arrayIndex = index - 1;

                        if (arrayIndex >= 0 && arrayIndex < args.Data.Count)
                        {
                            return args.Data[arrayIndex];
                        }
                    }
                    return m.Value;
                });
                return result;
            }
            return String.Empty;
        }

        /// <inheritdoc/>
        protected override void OnExecuteProgress(ExecuteProgressEventArgs args)
        {
            base.OnExecuteProgress(args);

            lock (_syncRoot)
            {
                _executeProgress = args.OverallPercentage;
                ReportProgress();

                var progress = GetProgress();

                if (Command.Display is Display.Embedded)
                    Engine.SendEmbeddedProgress(args.ProgressPercentage, progress);

                args.Cancel = CancelRequested;
            }
        }

        /// <inheritdoc/>
        protected override void OnPlanBegin(PlanBeginEventArgs args)
        {
            base.OnPlanBegin(args);

            ExecuteMessage?.Invoke(this, "正在初始化...");
        }

        /// <inheritdoc/>
        protected override void OnPlanComplete(PlanCompleteEventArgs args)
        {
            base.OnPlanComplete(args);

            _dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current?.MainWindow ?? new Window();
                var hwnd = new WindowInteropHelper(mainWindow).EnsureHandle();
                Engine.Apply(hwnd);
            });
        }

        /// <inheritdoc/>
        protected override void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            base.OnPlanMsiFeature(args);

            if (!_isAutoPlan)
            {
                PlanFeature?.Invoke(this, args);
                return;
            }

            if (RelatedBundle.FeatureStatus.TryGetValue(args.FeatureId, out bool isInstalled))
            {
                args.State = isInstalled ? FeatureState.Local : FeatureState.Absent;
                return;
            }

            args.State = FeatureState.Local;
        }

        /// <inheritdoc/>
        protected override void OnProgress(ProgressEventArgs args)
        {
            base.OnProgress(args);

            args.Cancel = CancelRequested;
        }

        /// <inheritdoc/>
        private void Plan(LaunchAction action)
        {
            Engine.Plan(action);
        }

        /// <inheritdoc/>
        private void ReportProgress()
        {
            var pct = GetProgress();

            if (CancelRequested) return;

            ProgressChanged?.Invoke(this, pct);
        }

        /// <inheritdoc/>
        private int GetProgress()
        {
            return (_cacheProgress + _executeProgress) / _progressPhases;
        }
    }
}

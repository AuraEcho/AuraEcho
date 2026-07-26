using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Hub;
using AuraEcho.Constants;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Services;
using AuraEcho.Core.Tools;
using AuraEcho.Core.Tools.HttpClientPipelines;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Logging;
using AuraEcho.Models;
using AuraEcho.Persistence;
using AuraEcho.Persistence.Contracts;
using AuraEcho.Persistence.Repositories;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Services;
using AuraEcho.Strings;
using AuraEcho.Telemetry;
using AuraEcho.Toolkit.Wpf.Imaging;
using AuraEcho.Toolkit.Wpf.RegionDialog;
using AuraEcho.Toolkit.Wpf.Services;
using AuraEcho.Tools;
using AuraEcho.ViewModels;
using AuraEcho.Views;
using DryIoc;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Serilog.Core;

namespace AuraEcho;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    private const string PIPE_NAME = "AURAECHO_APP_PIPE";
    private static Mutex _instanceMutex;
    private string[] _startupArgs;
    private TaskbarIcon _notifyIcon;
    private static ILoggerFactory _loggerFactory;
    private static LoggingLevelSwitch _logLevelSwitch;
    private static ILogger<App> _logger;
    private static ITelemetryService _telemetry;
    private static TelemetryContextFactory _telemetryContextFactory;
    private static Stopwatch _startupStopwatch;
    private static readonly Stopwatch _sessionStopwatch = Stopwatch.StartNew();
    private static MemorySampler _memorySampler;
    private static readonly HostDbContextProvider _hostDbContextProvider = new(ApplicationPaths.HostDataBase);
    protected override Window CreateShell()
    {
        LoggingAttribute.LoggerFactory = Container.Resolve<ILoggerFactory>();
        return Container.Resolve<MainWindow>();
    }

    public static bool ShutdownRequested { get; private set; }

    /// <summary>
    /// 获取或生成设备安装标识。优先从 SecureStore 读取，不存在则新建并持久化。
    /// </summary>
    private static Guid GetOrCreateInstallationId()
    {
        var existing = SecureStore.Load(SecureStoreKeys.InstallationId);
        if (Guid.TryParse(existing, out var id))
            return id;

        var newId = Guid.NewGuid();
        SecureStore.Save(SecureStoreKeys.InstallationId, newId.ToString());
        return newId;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<HostDbContext>(provider => _hostDbContextProvider.CreateDbContext());

        // Prism 的 IContainerRegistry 不支持开放泛型注册，使用底层 DryIoc 容器完成。
        containerRegistry.RegisterInstance(_loggerFactory);
        var container = containerRegistry.GetContainer();
        container.Register(typeof(ILogger<>), typeof(Logger<>), reuse: Reuse.Transient);

        containerRegistry.RegisterSingleton<IClock, ServerClock>();
        containerRegistry.RegisterSingleton<ITokenProvider, TokenProvider>();
        containerRegistry.RegisterSingleton<IHubTokenProvider>(c => c.Resolve<ITokenProvider>());
        containerRegistry.RegisterSingleton<IHubClient, HubClient>();

        containerRegistry.RegisterSingleton<ApiClient>(c =>
        {
            var logHandler = new LoggingHandler(c.Resolve<ILogger<LoggingHandler>>());
            var telemetryHandler = new TelemetryHandler(c.Resolve<ITelemetryService>());
            var serverTimeHandler = new ServerTimeHandler(c.Resolve<IClock>());
            var authHandler = c.Resolve<AuthHandler>();

            logHandler.InnerHandler = telemetryHandler;
            telemetryHandler.InnerHandler = serverTimeHandler;
            serverTimeHandler.InnerHandler = authHandler;
            authHandler.InnerHandler = new HttpClientHandler();

            return new ApiClient(logHandler);
        });

        containerRegistry.RegisterSingleton<IPathProvider, PathProvider>();
        containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
        containerRegistry.RegisterSingleton<IPluginManager, PluginManager>();
        containerRegistry.RegisterSingleton<IThemeManager, ThemeManager>();
        containerRegistry.RegisterSingleton<IHostSettingsProvider, HostSettingsProvider>();
        containerRegistry.RegisterSingleton<ILocalPluginRepository, LocalPluginRepository>();
        containerRegistry.RegisterSingleton<IUserAnnouncementRepository, UserAnnouncementRepository>();
        containerRegistry.RegisterSingleton<IAnnouncementService, AnnouncementService>();
        containerRegistry.RegisterSingleton<IRegionDialogService, RegionDialogService>();
        containerRegistry.RegisterSingleton<INavigationService, NavigationService>();
        containerRegistry.RegisterSingleton<IPluginInstallService, PluginInstallService>();
        containerRegistry.RegisterSingleton<IAuraToastService, AuraToastService>();

        containerRegistry.RegisterSingleton<ITransferManager, TransferManager>();
        containerRegistry.RegisterSingleton<IClientSession, ClientSession>();
        containerRegistry.RegisterSingleton<ILicenseService, HostLicenseService>();
        containerRegistry.RegisterSingleton<IPurchaseCoordinator, PurchaseCoordinator>();
        containerRegistry.RegisterSingleton<IWebImageLoader>(() => new WebImageLoader(ApplicationPaths.ImageCache));
        containerRegistry.RegisterSingleton<IPluginLoader, PluginLoader>();
        containerRegistry.RegisterSingleton<OrderPayUrlCacheService>();

        containerRegistry.RegisterSingleton<TelemetryStore>(c =>
            new TelemetryStore(ApplicationPaths.TelemetryDataBase));

        containerRegistry.RegisterSingleton<TelemetryContextFactory>(c =>
            new TelemetryContextFactory(GetOrCreateInstallationId));

        containerRegistry.RegisterSingleton<ITelemetryService>(c =>
        {
            var service = new TelemetryService(
                c.Resolve<TelemetryStore>(),
                c.Resolve<TelemetryContextFactory>());

            // 供遥测上报和异常处理器使用
            _telemetry = service;
            _telemetryContextFactory = c.Resolve<TelemetryContextFactory>();
            return service;
        });

        containerRegistry.RegisterSingleton<MemorySampler>();
        containerRegistry.RegisterSingleton<InteractionTracker>();
        containerRegistry.RegisterSingleton<GlobalExceptionHandler>();

        containerRegistry.RegisterForNavigation<Homepage>();
        containerRegistry.RegisterForNavigation<Settings>();
        containerRegistry.RegisterForNavigation<GeneralSettings>();
        containerRegistry.RegisterForNavigation<SendFeedback>();
        containerRegistry.RegisterForNavigation<ConfirmDialog>();
        containerRegistry.RegisterForNavigation<PluginsMarketplace>();
        containerRegistry.RegisterForNavigation<MarketplacePluginDetails>();
        containerRegistry.RegisterForNavigation<SignIn>();
        containerRegistry.RegisterForNavigation<ResetPassword>();
        containerRegistry.RegisterForNavigation<PasswordResetCompleted>();
        containerRegistry.RegisterForNavigation<ImageViewer>();
        containerRegistry.RegisterForNavigation<Purchase>();
        containerRegistry.RegisterForNavigation<AccountSettings>();
        containerRegistry.RegisterForNavigation<About>();
        containerRegistry.RegisterForNavigation<WebContainer>();
        containerRegistry.RegisterForNavigation<AutoSignIn>();
        containerRegistry.RegisterForNavigation<AnnouncementView>();
    }

    protected override void OnInitialized()
    {
        Container.Resolve<GlobalExceptionHandler>().Register();
        LoadConfig();

        Task.Run(() => _telemetry.TrackEvent("SessionStart", _telemetryContextFactory.Context.ToProperties()));

        WebImageLoaderContext.Default = Container.Resolve<IWebImageLoader>();

        if (_startupArgs.Contains("-hide")) return;

        base.OnInitialized();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _startupArgs = e.Args;

        base.OnStartup(e);

        StartPipeServer();

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.DataContext = Container.Resolve<NotifyIconViewModel>();

        Container.Resolve<IEventAggregator>().GetEvent<AppRestartEvent>().Subscribe(RestartApp);
        Container.Resolve<IEventAggregator>().GetEvent<AppShutdownEvent>().Subscribe(ShutdownApp);

        Container.Resolve<IPluginManager>().CleanOldPluginsAsync();

        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
        {
            string arg = (viewType.FullName.EndsWith("View") ? "Model" : "ViewModel");
            var viewModelName = viewType.FullName?
                .Replace(".Views.", ".ViewModels.") + arg;
            return viewType.Assembly.GetType(viewModelName);
        });

        _ = WebViewEnvironment.InitAllEnvironmentsAsync();

        // 记录启动耗时
        _startupStopwatch.Stop();
        _telemetry?.TrackMetric("App.StartupTime", new Dictionary<string, double> { ["value"] = _startupStopwatch.Elapsed.TotalMilliseconds });
        _telemetry?.TrackEvent("App.Launch");

        _memorySampler = Container.Resolve<MemorySampler>();
        _memorySampler.Start();

        // 启动后拉取一次公告
        _ = Container.Resolve<IAnnouncementService>().RefreshAsync();

        // 全局 UI 交互自动捕获
        Container.Resolve<InteractionTracker>().Register();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ToastNotificationManagerCompat.Uninstall();
        _sessionStopwatch.Stop();

        // 停止内存采样器
        _memorySampler?.StopAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

        _telemetry?.TrackMetric("App.SessionDuration", new Dictionary<string, double> { ["value"] = _sessionStopwatch.Elapsed.TotalSeconds });
        _telemetry?.TrackEvent("App.Shutdown");
        _logger.LogInformation("App.Shutdown");

        // 等待遥测数据缓存持久化
        if (_telemetry is TelemetryService telemetryService)
            telemetryService.FlushAndShutdownAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

        // 释放日志工厂，flush 异步 sink 并落盘剩余日志。
        _loggerFactory?.Dispose();

        base.OnExit(e);
    }


    private void LoadConfig()
    {
        var hostSettingsProvider = Container.Resolve<IHostSettingsProvider>();
        var hostSettings = hostSettingsProvider.LoadHostSettings();

        Container.Resolve<IThemeManager>().CurrentTheme = hostSettings.AppTheme;
        var targetCultureInfo = hostSettings.AppLanguage switch
        {
            AppLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
            AppLanguage.English => new CultureInfo("en-US"),
            AppLanguage.Korean => new CultureInfo("ko-KR"),
            AppLanguage.Japanese => new CultureInfo("ja-JP"),
            AppLanguage.ChineseTraditional => new CultureInfo("zh-TW"),
            _ => CultureInfo.CurrentCulture
        };
        LocalizationManager.ChangeCulture(targetCultureInfo);

        RenderOptions.ProcessRenderMode =
            hostSettings.HardwareAcceleration
            ? RenderMode.Default
            : RenderMode.SoftwareOnly;

        _telemetry.IsEnabled = hostSettings.TelemetryEnabled;
    }

    /// <summary>
    /// 程序入口函数
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        _startupStopwatch = Stopwatch.StartNew();
        _loggerFactory = SerilogConfigurator.CreateLoggerFactory(
            new LoggingOptions(ApplicationPaths.Logs, "client-", "Host"),
            out _logLevelSwitch);
        _logger = _loggerFactory.CreateLogger<App>();
        _logger.LogDebug("程序已启动");

        if (Mutex.TryOpenExisting(MutexNames.INSTALLER_MUTEX_ID, out var _))
        {
            _logger.LogDebug("检测到安装程序正在运行，正在退出程序。");
            return;
        }

#if !DEBUG
        _instanceMutex = new(true, MutexNames.AURAECHO_MUTEX_ID, out bool createdNew);
        if (!createdNew)
        {
            if (!args.Contains("-hide"))
            {
                using var client = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);
                client.Connect(200);
                using var writer = new StreamWriter(client);
                writer.WriteLine(NamedPipeMessages.ShowWindow);
                writer.Flush();
            }

            _logger.LogDebug("已有实例正在运行，正在退出程序。");
            return;
        }
#endif
        CreateDatabaseIfNotExists();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void CreateDatabaseIfNotExists()
    {
        if (File.Exists(ApplicationPaths.HostDataBase)) return;

        using var pluginDbContext = _hostDbContextProvider.CreateDbContext();

        _logger.LogInformation("Begin Migrate");
        pluginDbContext.Database.Migrate();
        _logger.LogInformation("End Migrate");
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
                using var server = NamedPipeServerStreamAcl.Create(
                    PIPE_NAME,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    pipeSecurity);

                await server.WaitForConnectionAsync();

                using var reader = new StreamReader(server);
                string? cmd = await reader.ReadLineAsync();

                if (String.IsNullOrWhiteSpace(cmd)) continue;

                _ = Task.Run(() => HandlePipeMessage(cmd));
            }
        });

        static void HandlePipeMessage(string pipeMessage)
        {
            switch (pipeMessage)
            {
                case NamedPipeMessages.ShowWindow:
                    {
                        RequestShowApp();
                        return;
                    }
                case var _ when pipeMessage.StartsWith("NewVersion:"):
                    {
                        var newVersionStr = pipeMessage["NewVersion:".Length..];
                        if (Version.TryParse(newVersionStr, out var newVersion))
                        {
                            NewVersionInstalled(newVersion);
                        }
                        return;
                    }
                case var _ when pipeMessage.StartsWith("PluginNewVersion:"):
                    {
                        var data = pipeMessage["PluginNewVersion:".Length..].Split(":");
                        if (data.Length < 2) return;

                        if (!Guid.TryParse(data[0], out Guid pluginId)) return;
                        if (!Version.TryParse(data[1], out Version? newVersion)) return;

                        PluginNewVersionInstalled(pluginId, newVersion);
                        return;
                    }
            }
        }

        static void RequestShowApp()
        {
            IEventAggregator eventAggregator = (Current as App)!.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<RequestShowAppEvent>().Publish();
        }

        static void NewVersionInstalled(Version newVersion)
        {
            IEventAggregator eventAggregator = (Current as App)!.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<RequestRestartAppEvent>().Publish(new AppUpdatePendingRestart
            {
                Id = Guid.NewGuid(),
                Title = "灵光回声",
                LatestVersion = newVersion
            });
        }

        static void PluginNewVersionInstalled(Guid pluginId, Version newVersion)
        {
            IEventAggregator eventAggregator = (Current as App)!.Container.Resolve<IEventAggregator>();
            IPluginManager pluginManager = (Current as App)!.Container.Resolve<IPluginManager>();

            AppPlugin? targetPlugin = pluginManager.Plugins.FirstOrDefault(p => p.PluginId == pluginId);
            if (targetPlugin is null) return;

            var targetPluginVersion = Version.Parse(targetPlugin.Version);
            if (newVersion <= targetPluginVersion) return;

            eventAggregator.GetEvent<RequestRestartAppEvent>().Publish(new PluginUpdatePendingRestart
            {
                Id = Guid.NewGuid(),
                IconPath = Path.Combine(targetPlugin.WorkingDirectory, targetPlugin.Icon),
                PluginId = targetPlugin.PluginId,
                Title = targetPlugin.PluginName,
                LatestVersion = newVersion,
                CurrentVersion = Version.Parse(targetPlugin.Version)
            });
        }
    }

    private static void RestartApp()
    {
        _telemetry?.TrackEvent("App.Restart");
        ExitInternal(true);
    }

    private static void ShutdownApp() => ExitInternal(false);

    private static void ExitInternal(bool isRestart)
    {
        if (ShutdownRequested) return;
        ShutdownRequested = true;

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();

        if (isRestart)
#if DEBUG
            Process.Start(Environment.ProcessPath!);
#else
            Process.Start(ApplicationPaths.LauncherPath, $"-oldpid={Environment.ProcessId}");
#endif

        Current.Shutdown();
    }
}

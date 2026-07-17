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
using System.Windows.Threading;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Hub;
using AuraEcho.Constants;
using AuraEcho.Core.Attributes;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Data;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Repositories;
using AuraEcho.Core.Services;
using AuraEcho.Core.Telemetry;
using AuraEcho.Core.Tools;
using AuraEcho.Core.Tools.HttpClientPipelines;
using AuraEcho.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.PluginContracts.Services;
using AuraEcho.Services;
using AuraEcho.Tools;
using AuraEcho.Strings;
using AuraEcho.UIToolkit.RegionDialog;
using AuraEcho.ViewModels;
using AuraEcho.Views;
using DryIoc;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;

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
    private static IAppLogger _logger;
    private static ITelemetryService _telemetry;
    private static Stopwatch _startupStopwatch;
    protected override Window CreateShell()
    {
        LoggingAttribute.Logger = Container.Resolve<IAppLogger>();
        return Container.Resolve<MainWindow>();
    }

    public static bool ShutdownRequested { get; private set; }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<HostDbContext>(provider => HostDbContextRuntimeFactory.CreateDbContext());

        containerRegistry.RegisterInstance(_logger);
        containerRegistry.RegisterSingleton<IClock, ServerClock>();
        containerRegistry.RegisterSingleton<ITokenProvider, TokenProvider>();
        containerRegistry.RegisterSingleton<IHubTokenProvider>(c => c.Resolve<ITokenProvider>());
        containerRegistry.RegisterSingleton<IHubClient, HubClient>();

        containerRegistry.RegisterSingleton<ApiClient>(c =>
        {
            var logHandler = new LoggingHandler(c.Resolve<IAppLogger>());
            var serverTimeHandler = new ServerTimeHandler(c.Resolve<IClock>());
            var authHandler = c.Resolve<AuthHandler>();

            logHandler.InnerHandler = serverTimeHandler;
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
        containerRegistry.RegisterSingleton<IRegionDialogService, RegionDialogService>();
        containerRegistry.RegisterSingleton<INavigationService, NavigationService>();
        containerRegistry.RegisterSingleton<IPluginInstallService, PluginInstallService>();
        containerRegistry.RegisterSingleton<IAuraToastService, AuraToastService>();

        containerRegistry.RegisterSingleton<ITransferManager, TransferManager>();
        containerRegistry.RegisterSingleton<IClientSession, ClientSession>();
        containerRegistry.RegisterSingleton<ILicenseService, HostLicenseService>();
        containerRegistry.RegisterSingleton<IPurchaseCoordinator, PurchaseCoordinator>();
        containerRegistry.RegisterSingleton<IWebImageLoader, WebImageLoader>();
        containerRegistry.RegisterSingleton<IPluginLoader, PluginLoader>();
        containerRegistry.RegisterSingleton<ISkuOrderCacheService, SkuOrderCacheService>();

        containerRegistry.RegisterSingleton<TelemetryStore>();
        containerRegistry.RegisterSingleton<TelemetryContextFactory>();
        containerRegistry.RegisterSingleton<ITelemetryService>(c =>
        {
            var service = new TelemetryService(c.Resolve<TelemetryStore>());

            // 供异常处理器使用
            _telemetry = service;
            return service;
        });

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
    }

    protected override void OnInitialized()
    {
        RegisterEvents();
        LoadConfig();
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
        _telemetry?.TrackMetric("App.StartupTime", _startupStopwatch.Elapsed.TotalMilliseconds);
        _telemetry?.TrackEvent("App.Launch");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ToastNotificationManagerCompat.Uninstall();
        _telemetry?.TrackEvent("App.Shutdown");
        _logger.Information("App.Shutdown");

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

        _telemetry?.IsEnabled = hostSettings.TelemetryEnabled;
    }

    /// <summary>
    /// 程序入口函数
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        _startupStopwatch = Stopwatch.StartNew();
        _logger = new Serilogger(ApplicationPaths.Logs);
        _logger.Debug("程序已启动");
        
        if (Mutex.TryOpenExisting(MutexNames.INSTALLER_MUTEX_ID, out var _))
        {
            _logger.Debug("检测到安装程序正在运行，正在退出程序。");
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

            _logger.Debug("已有实例正在运行，正在退出程序。");
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

        using var pluginDbContext = HostDbContextRuntimeFactory.CreateDbContext();

        _logger.Information("Begin Migrate");
        pluginDbContext.Database.Migrate();
        _logger.Information("End Migrate");
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
    /// <summary>
    /// 订阅全局异常处理事件
    /// </summary>
    private void RegisterEvents()
    {
        //Task线程内未捕获异常处理事件
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        //UI线程未捕获异常处理事件（UI主线程）
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }
    /// <summary>
    /// Task 线程异常处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            if (e.Exception is Exception exception)
            {
                HandleException(exception, "TaskScheduler");
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "TaskScheduler");
        }
        finally
        {
            e.SetObserved();
        }
    }

    /// <summary>
    /// UI 线程异常处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception, "Dispatcher");
        }
        catch (Exception ex)
        {
            HandleException(ex, "Dispatcher");
        }
        finally
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// 非 UI 线程异常处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception, "AppDomain");
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "AppDomain");
        }
        finally
        {

        }
    }

    /// <summary>
    /// 全局异常处理逻辑
    /// </summary>
    /// <param name="exception"></param>
    private void HandleException(Exception exception, string source = "Unknown")
    {
        _logger.Fatal($"未处理的应用程序异常: {exception}");

        try
        {
            _telemetry?.TrackException(exception, new Dictionary<string, string>
            {
                ["source"] = source
            });
        }
        catch
        {
        }
    }
}

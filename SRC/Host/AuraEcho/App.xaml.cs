using DryIoc;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using AuraEcho.Constants;
using AuraEcho.Core.Attributes;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Data;
using AuraEcho.Core.Events;
using AuraEcho.Core.Repositories;
using AuraEcho.Core.Services;
using AuraEcho.Core.Tools;
using AuraEcho.Core.Tools.HttpClientPipelines;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Services;
using AuraEcho.UIToolkit.RegionDialog;
using AuraEcho.ViewModels;
using AuraEcho.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AuraEcho;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private const string PIPE_NAME = "AuraEcho_SingleInstance_Pipe";

    private string[] _startupArgs;
    private TaskbarIcon _notifyIcon;
    private static IAppLogger _logger;
    protected override Window CreateShell()
    {
        LoggingAttribute.Logger = Container.Resolve<IAppLogger>();
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<AuraEchoDbContext>(provider => DbContextFactory.CreateDbContext());

        containerRegistry.RegisterSingleton<HttpClient>(c =>
        {
            var log = c.Resolve<LoggingHandler>();
            var serverTime = c.Resolve<ServerTimeHandler>();
            var auth = c.Resolve<AuthHandler>();

            log.InnerHandler = serverTime;
            serverTime.InnerHandler = auth;
            auth.InnerHandler = new HttpClientHandler();

            return new HttpClient(log);
        });

        containerRegistry.RegisterInstance(_logger);
        containerRegistry.RegisterSingleton<IClock, ServerClock>();
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

        containerRegistry.RegisterSingleton<IFileRepository, FileRepository>();
        containerRegistry.RegisterSingleton<ITransferManager, TransferManager>();
        containerRegistry.RegisterSingleton<IClientSession, ClientSession>();
        containerRegistry.RegisterSingleton<IAuthRepository, AuthRepository>();
        containerRegistry.RegisterSingleton<IAppPackageRepository, AppPackageRepository>();
        containerRegistry.RegisterSingleton<IRemotePluginRepository, RemotePluginRepository>();

        containerRegistry.RegisterForNavigation<Homepage>();
        containerRegistry.RegisterForNavigation<Settings>();
        containerRegistry.RegisterForNavigation<GeneralSettings>();
        containerRegistry.RegisterForNavigation<ConfirmDialog>();
        containerRegistry.RegisterForNavigation<PluginsMarketplace>();
        containerRegistry.RegisterForNavigation<MarketplacePluginDetails>();
        containerRegistry.RegisterForNavigation<SignIn>();
        containerRegistry.RegisterForNavigation<ResetPassword>();
        containerRegistry.RegisterForNavigation<PasswordResetCompleted>();
        containerRegistry.RegisterForNavigation<SignInExpired>();
    }

    protected override void OnInitialized()
    {
        RegisterEvents();
        LoadConfig();

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
            _ => CultureInfo.CurrentCulture
        };
        ApplicationResources.ChangeCulture(targetCultureInfo);

        RenderOptions.ProcessRenderMode =
            hostSettings.HardwareAcceleration
            ? RenderMode.Default
            : RenderMode.SoftwareOnly;
    }

    /// <summary>
    /// 程序入口函数
    /// </summary>
    [STAThread]
    static void Main()
    {
        _logger = new Serilogger(ApplicationPaths.Logs);
        _logger.Debug("程序已启动");

        if (Mutex.TryOpenExisting(MutexNames.INSTALLER_MUTEX_ID, out var _))
        {
            _logger.Debug("检测到安装程序正在运行，正在退出程序。");
            return;
        }

        using var mutex = new Mutex(true, MutexNames.AURAECHO_MUTEX_ID);
        if (!mutex.WaitOne(TimeSpan.Zero, true))
        {
            using var client = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);
            client.Connect(200);
            using var writer = new StreamWriter(client);
            writer.WriteLine(NamedPipeMessages.ShowWindow);
            writer.Flush();

            _logger.Debug("已有实例正在运行，正在退出程序。");
            return;
        }

        CreateDatabaseIfNotExists();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void CreateDatabaseIfNotExists()
    {
        if (File.Exists(ApplicationPaths.HostDataBase)) return;

        using var pluginDbContext = DbContextFactory.CreateDbContext();

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

                if (cmd == NamedPipeMessages.ShowWindow)
                {
                    _ = Task.Run(RequestShowApp);
                }
            }
        });
    }

    private static void RequestShowApp()
    {
        IEventAggregator eventAggregator = (Current as App)!.Container.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<RequestShowAppEvent>().Publish();
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
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
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
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
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
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {

        }
    }

    /// <summary>
    /// 全局异常处理逻辑
    /// </summary>
    /// <param name="exception"></param>
    private void HandleException(Exception exception)
    {
        _logger.Debug(exception.ToString());
    }
}

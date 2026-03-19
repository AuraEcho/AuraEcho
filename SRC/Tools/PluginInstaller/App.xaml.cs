using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DryIoc;
using Microsoft.EntityFrameworkCore;
using PluginInstaller.Tools;
using PluginInstaller.Views;
using AuraEcho.Core.Attributes;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Data;
using AuraEcho.Core.Native.Win32;
using AuraEcho.Core.Repositories;
using AuraEcho.Core.Services;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using AuraEcho.Core.Tools;

namespace PluginInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    private bool _isNoWindowMode;
    private static IAppLogger _logger;
    protected override Window CreateShell()
    {
        LoggingAttribute.Logger = Container.Resolve<IAppLogger>();
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<AuraEchoDbContext>(provider => DbContextFactory.CreateDbContext());

        containerRegistry.RegisterInstance(_logger);
        containerRegistry.RegisterSingleton<IPathProvider, PathProvider>();
        containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
        containerRegistry.RegisterSingleton<IRegionDialogService, RegionDialogService>();
        containerRegistry.RegisterSingleton<IPluginInstallService, PluginInstallService>();

        containerRegistry.RegisterForNavigation<InstallPreparation>();
        containerRegistry.RegisterForNavigation<Installing>();
        containerRegistry.RegisterForNavigation<InstallCompleted>();
        containerRegistry.RegisterForNavigation<PickPluginInstallFile>();
        containerRegistry.RegisterForNavigation<ConfirmDialog>();

        containerRegistry.Register<ILocalPluginRepository, LocalPluginRepository>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
    }

    protected override void OnInitialized()
    {
        if (_isNoWindowMode) return;

        base.OnInitialized();
    }

    private async Task QuietInstallPluginAsync(string pluginFile)
    {
        var logger = Container.Resolve<IAppLogger>();
        logger.Debug("Installing");
        
        if (await Container.Resolve<IPluginInstallService>().InstallAsync(pluginFile) is null)
        {
            logger.Error("Install failed");
            Shutdown();
            return;
        }

        logger.Debug("Install finished");
        logger.Debug("Start Shutdown");
        Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        GlobalObjectHolder.StartupArgs = e.Args;
        _isNoWindowMode = GlobalObjectHolder.StartupArgs.Contains("--nowindow");

        base.OnStartup(e);

        RegisterEvents();

        if (!File.Exists(ApplicationPaths.HostDataBase))
        {
            using var pluginDbContext = Container.Resolve<AuraEchoDbContext>();
            pluginDbContext.Database.Migrate();
        }

        var logger = Container.Resolve<IAppLogger>();
        logger.Debug($"ARGS: {String.Join(",", e.Args)}");
        if (!_isNoWindowMode)
        {
            logger.Debug("if (!_isNoWindowMode) => true");
            return;
        }


        var pluginFilePath = GlobalObjectHolder.StartupArgs.FirstOrDefault();
        if (!File.Exists(pluginFilePath))
        {
            Container.Resolve<IAppLogger>().Error($"File not found: {pluginFilePath}");
            Shutdown();
            return;
        }

        _ = QuietInstallPluginAsync(pluginFilePath);
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

        using var mutex = new Mutex(true, MutexNames.PLUGIN_INSTALLER_MUTEX_ID);
        if (!mutex.WaitOne(TimeSpan.Zero, true))
        {
            IntPtr mainWindowHandle = Win32Helper.FindWindow(null, "PlixInstaller");
            if (mainWindowHandle != IntPtr.Zero)
                Win32Helper.SetForegroundWindow(mainWindowHandle);

            _logger.Debug("已有实例正在运行，正在退出程序。");
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
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
    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
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
    }

    /// <summary>
    /// UI 线程异常处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
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
        Debug.WriteLine(exception);
        _logger.Debug(exception.ToString());
    }
}

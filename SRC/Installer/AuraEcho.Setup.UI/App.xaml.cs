using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Setup.UI.Views;
using AuraEcho.Setup.UI.WixToolset;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.Ioc;
using WixToolset.BootstrapperApplicationApi;

namespace AuraEcho.Setup.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<AuraEchoBootstrapper>(() => AuraEchoBootstrapper.Instance);

            containerRegistry.Register<IRegionDialogService, RegionDialogService>();
            containerRegistry.RegisterForNavigation<ConfirmDialog>();

            containerRegistry.RegisterForNavigation<InstallPreparation>();
            containerRegistry.RegisterForNavigation<Installing>();
            containerRegistry.RegisterForNavigation<InstallFinish>();
            containerRegistry.RegisterForNavigation<UninstallPreparation>();
            containerRegistry.RegisterForNavigation<Uninstalling>();
            containerRegistry.RegisterForNavigation<UninstallFinish>();
            containerRegistry.RegisterForNavigation<ActionCancelled>();
            containerRegistry.RegisterForNavigation<DowngradeDetected>();
        }

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        static void Main()
        {
            ManagedBootstrapperApplication.Run(AuraEchoBootstrapper.Instance);
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
            Debug.WriteLine(exception);
            AuraEchoBootstrapper.Instance.Engine.Log(LogLevel.Error, exception.ToString());
        }
    }
}
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Tools;
using AuraEcho.ViewModels;
using Microsoft.Web.WebView2.Core;
using Prism.Ioc;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AuraEcho.Views
{
    /// <summary>
    /// Interaction logic for WebContainer
    /// </summary>
    public partial class WebContainer : UserControl
    {
        public WebContainer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 阻止新窗口打开链接，改为在当前 WebView 中导航
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            WebContentRoot.CoreWebView2.Navigate(e.Uri);
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var telemetry = ContainerLocator.Container?.Resolve<ITelemetryService>();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await WebContentRoot.EnsureCoreWebView2Async(WebViewEnvironment.Default);
                stopwatch.Stop();
                WebContentRoot.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

                telemetry?.TrackMetric("WebView.ReadyDuration", stopwatch.Elapsed.TotalMilliseconds);

                if (DataContext is WebContainerViewModel vm)
                {
                    WebContentRoot.Source = new Uri(vm.SourceUri);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                telemetry?.TrackEvent("WebView.InitFailed", new System.Collections.Generic.Dictionary<string, string>
                {
                    ["exceptionType"] = ex.GetType().Name
                });
                Debug.WriteLine($"WebView2 初始化失败: {ex.Message}");
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void WebContentRoot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (WebContentRoot.CoreWebView2 is null) return;

            // 鼠标返回
            if (e.ChangedButton == MouseButton.XButton1 && WebContentRoot.CoreWebView2.CanGoBack)
            {
                WebContentRoot.CoreWebView2.GoBack();
                e.Handled = true;
                return;
            }

            // 鼠标前进
            if (e.ChangedButton == MouseButton.XButton2 && WebContentRoot.CoreWebView2.CanGoForward)
            {
                WebContentRoot.CoreWebView2.GoForward();
                e.Handled = true;
            }
        }
    }
}

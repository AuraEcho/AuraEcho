using AuraEcho.Tools;
using AuraEcho.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
            try
            {
                await WebContentRoot.EnsureCoreWebView2Async(WebViewEnvironment.Default);

                WebContentRoot.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

                if (DataContext is WebContainerViewModel vm)
                {
                    WebContentRoot.Source = new Uri(vm.SourceUri);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2 初始化失败: {ex.Message}");
            }
        }
    }
}

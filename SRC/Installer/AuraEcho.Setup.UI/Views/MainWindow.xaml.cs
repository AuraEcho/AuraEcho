using AuraEcho.Setup.UI.Models;
using AuraEcho.Setup.UI.ViewModels;
using Prism.Events;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;

namespace AuraEcho.Setup.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(IEventAggregator eventAggregator)
    {
        InitializeComponent();
    }

    private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Application.Current.Properties["MainWindowHandle"] = new WindowInteropHelper(this).Handle;

        // 用于在用户点击 UAC 弹窗按钮后将主窗口前置
        BringToForeground();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        var installState = vm!.InstallState;
        if (installState != InstallState.Applying)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        BringToForeground();
        vm.ExitCommand.Execute();
    }
    /// <summary>
    /// 使主窗口前置
    /// </summary>
    public void BringToForeground()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Topmost = true;
        Topmost = false;
        Focus();
    }
}

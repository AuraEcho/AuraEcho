using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using AuraEcho.Core.Attributes;
using AuraEcho.Core.Events;
using AuraEcho.ViewModels;
using Prism.Events;
namespace AuraEcho.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    /// 构造函数
    /// </summary>
    [Logging]
    public MainWindow(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<RequestShowAppEvent>().Subscribe(BringToForeground, ThreadOption.UIThread);
        InitializeComponent();
    }

    /// <summary>
    /// 使主窗口前置
    /// </summary>
    public void BringToForeground()
    {
        if (WindowState == WindowState.Minimized || !IsVisible)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    /// <summary>
    /// 阻止主窗口关闭
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.ShutdownRequested) return;

        e.Cancel = true;
        Hide();
        ShowToast();
    }


    /// <summary>
    /// 窗口加载完成事件处理程序
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 点击通知时, 激活程序(即使程序已关闭)
        // Listen to notification activation
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            //// Obtain the arguments from the notification
            //ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

            //// Obtain any user input (text boxes, menu selections) from the notification
            //ValueSet userInput = toastArgs.UserInput;
            //// 文本框内容
            //var textBoxContent = userInput["tbReply"].ToString();
            //// Need to dispatch to UI thread if performing UI operations
            //Application.Current.Dispatcher.Invoke(delegate
            //{
            //    // TODO: Show the corresponding content
            //    MessageBox.Show("Toast activated. Args: " + toastArgs.Argument);
            //});
        };
    }

    /// <summary>
    /// 窗口关闭按钮点击事件处理程序
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        ShowToast();
    }

    /// <summary>
    /// 托盘通知
    /// </summary>
    public static void ShowToast()
    {
        int conversationId = 384928;

        new ToastContentBuilder().AddArgument("conversationId", conversationId)
                                 .AddText("程序已最小化到系统托盘")
                                 .AddText("可转到个性化界面关闭推送通知")
                                 .Show();
    }

    private void MaxWin_MouseClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            return;
        }
    }
}

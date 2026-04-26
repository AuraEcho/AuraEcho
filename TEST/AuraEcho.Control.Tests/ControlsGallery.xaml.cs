using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Control.Tests
{
    /// <summary>
    /// ControlsGallery.xaml 的交互逻辑
    /// </summary>
    public partial class ControlsGallery : UserControl
    {
        public ControlsGallery()
        {
            InitializeComponent();
        }

        private void MaxWin_MouseClick(object sender, RoutedEventArgs e)
        {
            if (App.Current.MainWindow.WindowState == WindowState.Normal)
            {
                App.Current.MainWindow.WindowState = WindowState.Maximized;
                return;
            }

            if (App.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                App.Current.MainWindow.WindowState = WindowState.Normal;
                return;
            }
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }
    }
}

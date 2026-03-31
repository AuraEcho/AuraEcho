using System.Windows;
using System.Windows.Controls;
using AuraEcho.Setup.UI.ViewModels;

namespace AuraEcho.Setup.UI.Views
{
    /// <summary>
    /// Interaction logic for InstallFinish
    /// </summary>
    public partial class InstallFinish : UserControl
    {
        public InstallFinish()
        {
            InitializeComponent();
        }

        private void FinishedButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();

            (DataContext as InstallFinishViewModel).FinishedCommand.Execute();
        }
    }
}

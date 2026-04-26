using AuraEcho.Setup.UI.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Setup.UI.Views
{
    /// <summary>
    /// Interaction logic for InstallPreparation
    /// </summary>
    public partial class InstallPreparation : UserControl
    {
        public InstallPreparation()
        {
            InitializeComponent();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "请选择灵光回声的安装位置",
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                (DataContext as InstallPreparationViewModel)?.SetInstallFolderCommand.Execute(dialog.FileName);
            }
        }

        private void CustomInstallationHyperLink_Click(object sender, RoutedEventArgs e)
        {
            CustomInstallationLayout.Visibility = CustomInstallationLayout.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }
    }
}

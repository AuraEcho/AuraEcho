using System.Windows.Controls;
using AuraEcho.ViewModels;

namespace AuraEcho.Views
{
    /// <summary>
    /// Interaction logic for AccountSettings
    /// </summary>
    public partial class AccountSettings : UserControl
    {
        public AccountSettings()
        {
            InitializeComponent();
        }

        private void SelectAvatarFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择图像文件",
                Filter = "图像文件|*.jpg;*.png;*.jpeg;*.bmp",
            };
            if (dialog.ShowDialog() != true) return;

            (DataContext as AccountSettingsViewModel)?.UploadAvatarCommand.Execute(dialog.FileName);
        }
    }
}

using System.Diagnostics;
using System.Windows.Controls;
using AuraEcho.Core.Strings;
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
                Title = Labels.AccountSettings_SelectImageFile,
                Filter = Labels.AccountSettings_ImageFileFilter,
            };
            if (dialog.ShowDialog() != true) return;

            (DataContext as AccountSettingsViewModel)?.UploadAvatarCommand.Execute(dialog.FileName);
        }
    }
}

using System.Diagnostics;
using System.Windows.Controls;
using AuraEcho.Strings;
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
            if (DataContext is not AccountSettingsViewModel vm) return;

            if (vm.IsUploadingAvatar) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Labels.AccountSettings_SelectImageFile,
                Filter = Labels.AccountSettings_ImageFileFilter,
            };
            if (dialog.ShowDialog() != true) return;

            vm.UploadAvatarCommand.Execute(dialog.FileName);
        }
    }
}

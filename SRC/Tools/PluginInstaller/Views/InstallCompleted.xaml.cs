using System.Windows;
using System.Windows.Controls;

namespace PluginInstaller.Views;

/// <summary>
/// Interaction logic for InstallCompleted
/// </summary>
public partial class InstallCompleted : UserControl
{
    public InstallCompleted()
    {
        InitializeComponent();
    }

    private void FinishedButon_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

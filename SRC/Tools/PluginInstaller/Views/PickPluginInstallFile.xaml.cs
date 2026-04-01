using PluginInstaller.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PluginInstaller.Views;

/// <summary>
/// Interaction logic for PickPluginInstallFile
/// </summary>
public partial class PickPluginInstallFile : UserControl
{
    public PickPluginInstallFile()
    {
        InitializeComponent();
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length <= 0) return;

        if (String.Equals(Path.GetExtension(files[0]), ".plix", StringComparison.OrdinalIgnoreCase))
        {
            var command = (DataContext as PickPluginInstallFileViewModel).NavigationToInstallPreparationCommand;
            command.Execute(files[0]);
        }
    }

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length <= 0)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (String.Equals(Path.GetExtension(files[0]), ".plix", StringComparison.OrdinalIgnoreCase))
        {
            e.Effects = DragDropEffects.Copy; 
            e.Handled = true;
            return;
        }
    }
}

using AuraEcho.Toolkit.Wpf.Services;
using Microsoft.Win32;

namespace AuraEcho.Toolkit.Wpf.Services;

/// <summary>
/// 文件对话框服务
/// </summary>
public class FileDialogService : IFileDialogService
{
    /// <summary>
    /// 打开文件对话框
    /// </summary>
    public string? OpenFile(string dialogTitle, string filter = "All Files|*.*")
    {
        var dialog = new OpenFileDialog
        {
            Title = dialogTitle,
            Filter = filter,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// 选择文件(多选)
    /// </summary>
    public string[] OpenFiles(string dialogTitle, string filter = "All Files|*.*")
    {
        var dialog = new OpenFileDialog
        {
            Title = dialogTitle,
            Filter = filter,
            Multiselect = true
        };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    /// <summary>
    /// 打开文件夹选择对话框
    /// </summary>
    public string? SelectFolder(string dialogTitle)
    {
        var dialog = new OpenFolderDialog
        {
            Title = dialogTitle,
            InitialDirectory = "C:\\"
        };
        if (dialog.ShowDialog() == true)
        {
            return dialog.FolderName;
        }

        return null;
    }

    public string[] SelectFolders(string dialogTitle)
    {
        var dialog = new OpenFolderDialog
        {
            Title = dialogTitle,
            Multiselect = true,
            InitialDirectory = "C:\\"
        };

        return dialog.ShowDialog() == true ? dialog.FolderNames : [];
    }
}

namespace AuraEcho.Toolkit.Wpf.Services;

/// <summary>
/// 文件对话框服务接口
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 打开文件对话框
    /// </summary>
    string? OpenFile(string dialogTitle, string filter = "All Files|*.*");

    /// <summary>
    /// 选择文件(多选)
    /// </summary>
    string[] OpenFiles(string dialogTitle, string filter = "All Files|*.*");

    /// <summary>
    /// 打开文件夹选择对话框
    /// </summary>
    string? SelectFolder(string dialogTitle);

    /// <summary>
    /// 选择目录(多选)
    /// </summary>
    string[] SelectFolders(string dialogTitle);
}

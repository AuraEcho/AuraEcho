using System.Collections.ObjectModel;
using System.IO;

namespace PluginPacker.Models;

public class PluginFolder : PluginItem
{
    #region private members
    private string _folderPath;
    public ObservableCollection<PluginItem> _children = [];
    #endregion

    public PluginFolder Parent { get; set; }

    public ObservableCollection<PluginItem> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    public string FolderPath
    {
        get => _folderPath;
        set => SetProperty(ref _folderPath, value);
    }

    public PluginFolder(string folderPath, PluginFolder parent)
    {
        Type = PluginItemType.Folder;
        FolderPath = folderPath;
        Parent = parent;

        if (!String.IsNullOrWhiteSpace(FolderPath))
        {
            Name = Path.GetFileName(FolderPath);
        }

        InitChildrenFiles();
    }

    private void InitChildrenFiles()
    {
        if (String.IsNullOrWhiteSpace(FolderPath)) return;

        string[] files = Directory.GetFiles(FolderPath);
        List<PluginFile> pluginFiles = [.. files.Select(file => new PluginFile(file, Path.GetFileName(file), this))];
        pluginFiles.ForEach(Children.Add);

        string[] folders = Directory.GetDirectories(FolderPath);
        if (folders is null || folders.Length <= 0) return;

        List<PluginFolder> subFolders = [.. folders.Select(folder => new PluginFolder(folder, this))];
        subFolders.ForEach(Children.Add);
    }

    public void Add(PluginFolder subFolder)
    {
        Children.Add(subFolder);
    }

    public void Add(PluginFile file)
    {
        Children.Add(file);
    }
}

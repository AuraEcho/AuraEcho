namespace PluginPacker.Models;

public class PluginFile : PluginItem
{
    private bool _isEntryFile;

    public string FilePath { get; }

    public bool IsEntryFile
    {
        get => _isEntryFile;
        set => SetProperty(ref _isEntryFile, value);
    }

    public PluginFolder Parent { get; set; }

    public PluginFile(string filePath, string fileName, PluginFolder folder) : base(fileName)
    {
        Type = PluginItemType.File;
        FilePath = filePath;

        Parent = folder;
    }
}

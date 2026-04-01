using PluginPacker.Models;
using System.Windows;
using System.Windows.Controls;

namespace PluginPacker.Selector;

public class PluginItemDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate FileDataTemplate { get; set; }
    public DataTemplate FolderDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is not PluginItem pi) return null;

        if (pi.Type == PluginItemType.File) return FileDataTemplate;
        if (pi.Type == PluginItemType.Folder) return FolderDataTemplate;

        return null;
    }
}

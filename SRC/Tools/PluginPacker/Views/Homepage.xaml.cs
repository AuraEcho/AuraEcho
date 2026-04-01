using PluginPacker.Models;
using PluginPacker.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PluginPacker.Views;

/// <summary>
/// Interaction logic for Homepage
/// </summary>
public partial class Homepage : UserControl
{
    public Homepage()
    {
        InitializeComponent();
    }

    private void PluginFolder_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))  // 判断是否是文件拖放
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);  // 获取文件路径
            var targetNode = (PluginItem)(e.OriginalSource as FrameworkElement).DataContext;  // 获取目标目录节点
            PluginFolder targetPluginFolder =
                targetNode.Type == PluginItemType.File
                ? (targetNode as PluginFile).Parent
                : targetNode as PluginFolder;

            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    var newFolder = new PluginFolder(file, targetPluginFolder);
                    targetPluginFolder.Children.Add(newFolder);
                    continue;
                }

                // 在目标目录下添加文件
                var newFileNode = new PluginFile(file, Path.GetFileName(file), targetPluginFolder);  // 使用文件名创建新节点
                targetPluginFolder.Children.Add(newFileNode);
            }
        }
    }

    private void PluginFolder_DropOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // 设置拖放效果
            e.Effects = DragDropEffects.Copy;  // 这里设置为拷贝效果，可以改为 Move
            e.Handled = true;  // 表示事件已经处理
            return;
        }

        e.Effects = DragDropEffects.None;
    }

    private void PluginFilesTreeView_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not HomepageViewModel hvm) return;

        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        foreach (var file in files)
        {
            if (Directory.Exists(file))
            {
                var newFolder = new PluginFolder(file, hvm.RootFolder);
                hvm.RootFolder.Children.Add(newFolder);
                continue;
            }

            // 在目标目录下添加文件
            var newFileNode = new PluginFile(file, Path.GetFileName(file), hvm.RootFolder);  // 使用文件名创建新节点
            hvm.RootFolder.Children.Add(newFileNode);
        }
    }
}

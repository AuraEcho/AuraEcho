using Prism.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AuraEcho.Tools;

public static class GlobalCommands
{
    static GlobalCommands()
    {
        OpenInternalFileCommand = new DelegateCommand<string>(OpenInternalFile);
        OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
    }

    public static DelegateCommand<string> OpenUrlCommand { get; }
    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    public static DelegateCommand<string> OpenInternalFileCommand { get; }
    private static void OpenInternalFile(string relativeFilePath)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, relativeFilePath);

        Task.Run(() =>
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = filePath
            }));
    }
}

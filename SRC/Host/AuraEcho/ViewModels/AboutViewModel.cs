using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace AuraEcho.ViewModels;

public class AboutViewModel : BindableBase
{
    public Version CurrentVersion
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand<string> OpenUrlCommand { get; }
    private void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    public DelegateCommand<string> OpenFileCommand { get; }
    private void OpenFile(string relativeFilePath)
    {
        string currentFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        string filePath = Path.Combine(currentFolderPath, relativeFilePath);

        Task.Run(() =>
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = filePath
            }));
    }

    public AboutViewModel()
    {
        OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
        OpenFileCommand = new DelegateCommand<string>(OpenFile);

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
    }
}

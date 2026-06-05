using System.Diagnostics;
using System.Windows;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models;

public class StandalonePlugin : AppPlugin
{
    /// <summary>
    /// 入口文件
    /// </summary>
    public string? EntryFileName
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string? CommandLineArgs
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsRunning => Process != null && !Process.HasExited;

    public Process? Process
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public StandalonePlugin(StandalonePluginManifest manifest) : base(manifest)
    {
        EntryFileName = manifest.EntryFileName;
        CommandLineArgs = manifest.CommandLineArgs;
    }

    public void Open()
    {
        if (String.IsNullOrEmpty(EntryFileName)) return;
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = EntryFileName,
                Arguments = CommandLineArgs ?? string.Empty,
                WorkingDirectory = WorkingDirectory,
                UseShellExecute = true
            }
        };
        Process.Start();
    }

    public override AppSettingsItem? GetSettings() => null;

    public override ResourceDictionary? GetThemeResource(AppTheme theme) => null;
}

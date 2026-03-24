using Microsoft.Win32;
using System.IO;

namespace AuraEcho.Core.Tools;

/// <summary>
/// 程序路径常量
/// </summary>
public static class ApplicationPaths
{
    public static string BasePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "AuraEcho", "Client");

    public static string Plugins => Path.Combine(BasePath, "Plugins");
    public static string Logs => Path.Combine(BasePath, "Logs");
    public static string Temp => Path.Combine(BasePath, "Temp");
    public static string Data => Path.Combine(BasePath, "Data");
    public static string Config => Path.Combine(BasePath, "Config");
    public static string SecureStore => Path.Combine(BasePath, "SecureStore");
    public static string HostSettings => Path.Combine(Config, "global.cfg");
    public static string HostDataBase => Path.Combine(Data, "auraecho.db");
    public static string GetPluginPath(Guid pluginId) => Path.Combine(Plugins, pluginId.ToString());
    public static string LauncherPath { get; }
    public static string AppPath { get; }

    static ApplicationPaths()
    {
        Directory.CreateDirectory(Plugins);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Temp);
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Config);
        Directory.CreateDirectory(SecureStore);

        LauncherPath = GetLauncherPath();
        AppPath = GetAppPath();
    }

    private static string GetLauncherPath()
        => GetPathFromRegistry("LauncherPath");

    private static string GetAppPath()
        => GetPathFromRegistry("AppPath");

    private static string GetPathFromRegistry(string valueName)
    {
        const string keyPath = @"Software\AuraEcho";
        using RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath);
        if (key == null) return null;
        object value = key.GetValue(valueName);
        return value?.ToString();
    }
}

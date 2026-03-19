using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Interfaces;
using Serilog;
using System.IO;
using System.Text.Json;

namespace AuraEcho.Core.Services;

public class HostSettingsProvider(IAppLogger logger) : IHostSettingsProvider
{
    private readonly IAppLogger _logger = logger;

    public HostSettings LoadHostSettings()
    {
        if (!File.Exists(ApplicationPaths.HostSettings))
        {
            SaveHostSettings(HostSettings.Default);
            return HostSettings.Default;
        }

        string hostSettingsJson = File.ReadAllText(ApplicationPaths.HostSettings);
        var hostSettings = JsonSerializer.Deserialize<HostSettings>(hostSettingsJson);

        if (hostSettings == null)
        {
            _logger.Error("无法解析插件注册表。");
            SaveHostSettings(HostSettings.Default);
            return HostSettings.Default;
        }

        return hostSettings;
    }

    public void SaveHostSettings(HostSettings settings)
    {
        string pluginRegistriesJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ApplicationPaths.HostSettings, pluginRegistriesJson);
    }
}

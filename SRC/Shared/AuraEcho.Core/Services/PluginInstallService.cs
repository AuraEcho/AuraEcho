using System.IO;
using System.IO.Compression;
using System.Text.Json;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Domain;
using AuraEcho.Persistence.Contracts;
using AuraEcho.Telemetry;
using AuraEcho.Toolkit;
using Microsoft.Extensions.Logging;

namespace AuraEcho.Core.Services;

public class PluginInstallService : IPluginInstallService
{
    private const string MANIFEST_FILE_NAME = "plugin.manifest.json";
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly ILogger<PluginInstallService> _logger;
    private readonly ITelemetryService _telemetry;
    public PluginInstallService(ILocalPluginRepository localPluginRepository, ILogger<PluginInstallService> logger, ITelemetryService telemetry)
    {
        _localPluginRepository = localPluginRepository;
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// 安装插件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <remarks>TODO: 优化升级逻辑</remarks>
    public async Task<InstalledPluginModel> InstallAsync(string filePath)
    {        
        // 解压插件到临时目录
        var extractPath = Path.Combine(ApplicationPaths.Temp, "PluginInstall_" + Guid.NewGuid());
        ZipFile.ExtractToDirectory(filePath, extractPath);

        // 读取并解析 manifest 文件
        string manifestPath = Path.Combine(extractPath, MANIFEST_FILE_NAME);
        if (!File.Exists(manifestPath))
        {
            _logger.LogError("插件缺少 manifest 文件。");
            Directory.Delete(extractPath, true);
            return null;
        }

        string manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

        // 拷贝到目标插件目录
        string finalFolderPath = Path.Combine(ApplicationPaths.Plugins, manifest.Id.ToString("N"), manifest.Version);
        if (Directory.Exists(finalFolderPath))
            Directory.Delete(finalFolderPath, true);
        DirectoryUtils.SafeMoveDirectory(extractPath, finalFolderPath);

        _logger.LogDebug("查询已安装信息");
        var installedPlugin = (await _localPluginRepository.GetLocalPluginsAsync()).FirstOrDefault(pr => pr.Id == manifest.Id);
        if (installedPlugin is not null)
        {
            _logger.LogDebug("正在更新插件信息");
            await _localPluginRepository.UpdateLocalPluginAsync(new InstalledPluginModel
            {
                Id = installedPlugin.Id,
                InstaledAt = DateTime.UtcNow,
                PluginId = manifest.Id,
                PluginType = manifest.Type,
                Version = manifest.Version,
                InstallPath = finalFolderPath,
            });
        }
        else
        {
            installedPlugin = new InstalledPluginModel
            {
                Id = manifest.Id,
                InstaledAt = DateTime.UtcNow,
                PluginId = manifest.Id,
                InstallPath = finalFolderPath,
                PluginType = manifest.Type,
                Version = manifest.Version,
            };
            await _localPluginRepository.AddLocalPluginAsync(installedPlugin);
        }

        _logger.LogDebug("安装成功");
        _telemetry.TrackEvent("Plugin.Installed", new Dictionary<string, string>
        {
            ["pluginId"] = manifest.Id.ToString("D"),
            ["pluginType"] = manifest.Type.ToString(),
            ["version"] = manifest.Version
        });
        return installedPlugin;
    }
}

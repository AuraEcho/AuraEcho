using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Ioc;
using Serilog;

namespace AuraEcho.Core.Services;

public class PluginInstallService : IPluginInstallService
{
    private const string MANIFEST_FILE_NAME = "plugin.manifest.json";
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly IContainerProvider _containerProvider;
    private readonly IAppLogger _logger;
    public PluginInstallService(ILocalPluginRepository localPluginRepository, IContainerProvider containerProvider, IAppLogger logger)
    {
        _localPluginRepository = localPluginRepository;
        _containerProvider = containerProvider;
        _logger = logger;
    }

    /// <summary>
    /// 安装插件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <remarks>TODO: 优化升级逻辑</remarks>
    public async Task<LocalPluginModel> InstallAsync(string filePath)
    {        
        // 解压插件到临时目录
        var extractPath = Path.Combine(ApplicationPaths.Temp, "PluginInstall_" + Guid.NewGuid());
        ZipFile.ExtractToDirectory(filePath, extractPath);

        // 读取并解析 manifest 文件
        string manifestPath = Path.Combine(extractPath, MANIFEST_FILE_NAME);
        if (!File.Exists(manifestPath))
        {
            _logger.Error("插件缺少 manifest 文件。");
            Directory.Delete(extractPath, true);
            return null;
        }

        string manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

        // 拷贝到目标插件目录
        string finalFolderPath = Path.Combine(ApplicationPaths.Plugins, manifest.Id.ToString("N"), manifest.Version);
        DirectoryUtils.SafeMoveDirectory(extractPath, finalFolderPath);

        _logger.Error("查询已安装信息");
        var localPluginModel = (await _localPluginRepository.GetLocalPluginsAsync()).FirstOrDefault(pr => pr.Manifest.Id == manifest.Id);
        if (localPluginModel is not null)
        {
            _logger.Error("正在更新插件信息");
            await _localPluginRepository.UpdateLocalPluginAsync(new LocalPluginModel
            {
                Id = localPluginModel.Id,
                Manifest = manifest,
                PluginFolder = finalFolderPath,
            });
        }
        else
        {
            localPluginModel = new LocalPluginModel
            {
                Id = manifest.Id,
                Manifest = manifest,
                PluginFolder = finalFolderPath,
            };
            await _localPluginRepository.AddLocalPluginAsync(localPluginModel);
        }

        _logger.Debug("加载程序集");
        var entryAssemblyPath = Path.Combine(finalFolderPath, manifest.EntryAssemblyName);
        var alc = new PluginLoadContext(entryAssemblyPath);
        Assembly pluginAssembly = null;
        try
        {
            pluginAssembly = alc.LoadFromAssemblyPath(entryAssemblyPath);
        }
        catch (Exception ex)
        {
            _logger.Error($"加载插件程序集失败：{manifest.PluginName}，异常：{ex.Message}");
            return null;
        }
        _logger.Debug("执行插件环境初始化");
        IPluginSetup pluginDatabaseInitializer = GetPluginDatabaseInitializer(pluginAssembly);
        pluginDatabaseInitializer?.Setup(_containerProvider);

        _logger.Debug("安装成功");
        return localPluginModel;

        IPluginSetup GetPluginDatabaseInitializer(Assembly pluginAssembly)
        {
            Type? pluginDatabaseInitializerType =
                pluginAssembly.GetExportedTypes()
                              .Where(t => typeof(IPluginSetup).IsAssignableFrom(t))
                              .Where(t => t != typeof(IPluginSetup))
                              .SingleOrDefault(t => !t.IsAbstract);
            return _containerProvider.Resolve(pluginDatabaseInitializerType) as IPluginSetup;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Ioc;

namespace AuraEcho.Services;

public class PluginLoader : IPluginLoader
{
    private readonly IAppLogger _logger;
    private readonly IContainerProvider _containerProvider;
    private readonly ILocalPluginRepository _pluginRepository;
    private readonly List<PluginLoadContext> _pluginLoadContexts = [];
    public PluginLoader(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _pluginRepository = containerProvider.Resolve<ILocalPluginRepository>();
        _logger = _containerProvider.Resolve<IAppLogger>();
    }

    public async Task<AppPlugin> LoadPluginAsync(UserPluginModel userPluginModel)
    {
        if (userPluginModel is null || userPluginModel.LocalPlugin is null)
            throw new ArgumentException("参数不能为空");

        switch (userPluginModel.LocalPlugin.PluginType)
        {
            case PluginType.Native:
                var nativePlugin = await LoadNativePluginAsync(userPluginModel);
                return nativePlugin;
            case PluginType.Standalone:
                var standalonePlugin = await LoadStandalonePluginAsync(userPluginModel);
                return standalonePlugin;
            default:
                throw new NotSupportedException($"不支持的插件类型：{userPluginModel.LocalPlugin.PluginType}");
        }
    }

    public async Task<NativePlugin> LoadNativePluginAsync(UserPluginModel userPluginModel)
    {
        // 读取 manifest.json 文件
        string manifestPath = Path.Combine(
            userPluginModel.LocalPlugin.InstallPath,
            "plugin.manifest.json");

        PluginManifest pluginManifest =
            JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath))
            ?? throw new Exception("读取插件清单失败");

        string entryAssemblyPath = Path.Combine(
            userPluginModel.LocalPlugin.InstallPath,
            pluginManifest.EntryFileName);

        if (!File.Exists(entryAssemblyPath))
        {
            string errorMessage = $"插件 {userPluginModel.LocalPlugin.PluginId} 主程序集不存在：{entryAssemblyPath}";
            _logger.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        var alc = new PluginLoadContext(entryAssemblyPath);
        _pluginLoadContexts.Add(alc);
        Assembly pluginAssembly;
        try
        {
            pluginAssembly = alc.LoadEntryAssembly(entryAssemblyPath);
        }
        catch (Exception ex)
        {
            _logger.Error($"加载插件程序集失败：{userPluginModel.LocalPlugin.PluginId}，异常：{ex.Message}");
            return null;
        }

        IPlugin pluginContext = LoadPluginByAssembly(pluginAssembly);

        var nativePlugin = new NativePlugin(pluginManifest)
        {
            PluginType = PluginType.Native,
            WorkingDirectory = userPluginModel.LocalPlugin.InstallPath,
            PluginContext = pluginContext,
            PlanStatus = userPluginModel.Status
        };

        _logger.Debug("执行插件环境初始化");

        if (!userPluginModel.LocalPlugin.IsSetup)
        {
            await pluginContext.SetupAsync(_containerProvider);

            userPluginModel.LocalPlugin.IsSetup = true;
            await _pluginRepository.UpdateLocalPluginAsync(userPluginModel.LocalPlugin);
        }

        return nativePlugin;
 
        IPlugin LoadPluginByAssembly(Assembly pluginAssembly)
        {
            var pluginType = pluginAssembly.GetExportedTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t))
                .Where(t => t != typeof(IPlugin))
                .SingleOrDefault(t => !t.IsAbstract);

            if (pluginType == null)
                return null;

            var plugin = (IPlugin)Activator.CreateInstance(pluginType);
            plugin.RegisterTypes((IContainerRegistry)_containerProvider);
            plugin.OnInitialized(_containerProvider);
            return plugin;
        }
    }
    public async Task<StandalonePlugin> LoadStandalonePluginAsync(UserPluginModel userPluginModel)
    {
        // 读取 manifest.json 文件
        string manifestPath = Path.Combine(
            userPluginModel.LocalPlugin.InstallPath,
            "plugin.manifest.json");

        StandalonePluginManifest pluginManifest =
            JsonSerializer.Deserialize<StandalonePluginManifest>(File.ReadAllText(manifestPath))
            ?? throw new Exception("读取插件清单失败");

        string entryAssemblyPath = Path.Combine(
            userPluginModel.LocalPlugin.InstallPath,
            pluginManifest.EntryFileName);

        if (!File.Exists(entryAssemblyPath))
        {
            string errorMessage = $"插件 {userPluginModel.LocalPlugin.PluginId} 主程序集不存在：{entryAssemblyPath}";
            _logger.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        var standalonePlugin = new StandalonePlugin(pluginManifest)
        {
            WorkingDirectory = userPluginModel.LocalPlugin.InstallPath,
            PlanStatus = userPluginModel.Status
        };

        return standalonePlugin;
    }
}
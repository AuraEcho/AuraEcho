using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Attributes;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Modularity;

namespace AuraEcho.Services;

public class PluginManager : IPluginManager
{
    private bool _isInitialized;
    private readonly IModuleManager _moduleManager;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly ILocalPluginRepository _pluginRepository;
    private readonly IAppLogger _logger;
    private readonly List<PluginLoadContext> _pluginLoadContexts = [];

    private List<PluginRegistryModel> _plugins;
    public List<PluginRegistryModel> Plugins
    {
        get => _isInitialized ? _plugins : [];
    }

    public PluginManager(IModuleManager moduleManager, IModuleCatalog moduleCatalog, ILocalPluginRepository pluginRepository, IAppLogger logger)
    {
        _moduleManager = moduleManager;
        _moduleCatalog = moduleCatalog;
        _pluginRepository = pluginRepository;
        _logger = logger;
    }

    /// <summary>
    /// 加载所有插件并返回插件信息
    /// </summary>
    /// <returns></returns>
    public List<PluginRegistryModel> LoadPlugins()
    {
        // TODO: 线程安全

        if (_isInitialized)
        {
            _logger.Debug("插件管理器已初始化，跳过加载。");
            return _plugins;
        }

        _plugins = [];
        foreach (var pluginRegistry in _pluginRepository.GetPluginRegistries())
        {
            LoadPlugin(pluginRegistry);
        }
        _logger.Debug($"已加载 {_plugins.Count} 个插件。");

        _isInitialized = true;
        return _plugins;
    }
    public Task<List<PluginRegistryModel>> LoadPluginsAsync()
    {
        return Task.Run(LoadPlugins);
    }

    public bool LoadPlugin(PluginRegistryModel pluginRegistryModel)
    {
        if (pluginRegistryModel.PlanStatus == PluginPlanStatus.UninstallPending)
        {
            if (Directory.Exists(pluginRegistryModel.PluginFolder))
            {
                Directory.Delete(pluginRegistryModel.PluginFolder, true);
            }
            _pluginRepository.RemovePluginRegistry(pluginRegistryModel.Id);
            _logger.Debug($"插件 {pluginRegistryModel.Manifest.PluginName} 已被卸载，跳过加载。");
            return false;
        }

        string entryAssemblyPath = Path.Combine(ApplicationPaths.GetPluginPath(pluginRegistryModel.Manifest.Id), pluginRegistryModel.Manifest.EntryAssemblyName);

        if (!File.Exists(entryAssemblyPath))
        {
            _logger.Error($"插件 {pluginRegistryModel.Manifest.PluginName} 主程序集不存在：{entryAssemblyPath}");
            return false;
        }

        var alc = new PluginLoadContext(entryAssemblyPath);
        _pluginLoadContexts.Add(alc);
        Assembly pluginAssembly;
        try
        {
            pluginAssembly = alc.LoadFromAssemblyPath(entryAssemblyPath);
        }
        catch (Exception ex)
        {
            _logger.Error($"加载插件程序集失败：{pluginRegistryModel.Manifest.PluginName}，异常：{ex.Message}");
            return false;
        }

        PluginDefaultViewAttribute defaultView = pluginAssembly.GetCustomAttributes<PluginDefaultViewAttribute>().FirstOrDefault();
        if (defaultView is null)
        {
            _logger.Error($"插件 {pluginRegistryModel.Manifest.PluginName} 没有指定默认视图。");
            return false;
        }

        IPlugin pluginContext = LoadPluginByAssembly(pluginAssembly);
        pluginRegistryModel.PluginContext = pluginContext;

        _plugins.Add(pluginRegistryModel);
        return true;
    }
    public Task<bool> LoadPluginAsync(PluginRegistryModel pluginRegistryModel)
        => Task.Run(() => LoadPlugin(pluginRegistryModel));

    private IPlugin LoadPluginByAssembly(Assembly pluginAssembly)
    {
        var pluginType = pluginAssembly.GetExportedTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t))
            .Where(t => t != typeof(IPlugin))
            .Where(t => !t.IsAbstract)
            .SingleOrDefault();

        ModuleInfo moduleInfo = CreateModuleInfo(pluginType);
        _moduleCatalog.AddModule(moduleInfo);
        _moduleManager.LoadModule(moduleInfo.ModuleName);

        return (IPlugin)Activator.CreateInstance(pluginType);
    }

    private static ModuleInfo CreateModuleInfo(Type type)
    {
        string moduleName = type.Name;

        var moduleAttribute = CustomAttributeData.GetCustomAttributes(type)
            .FirstOrDefault(cad => cad.Constructor.DeclaringType.FullName == typeof(ModuleAttribute).FullName);

        if (moduleAttribute != null)
        {
            foreach (CustomAttributeNamedArgument argument in moduleAttribute.NamedArguments)
            {
                if (argument.MemberInfo.Name == "ModuleName")
                {
                    moduleName = (string)argument.TypedValue.Value;
                    break;
                }
            }
        }

        return new ModuleInfo(moduleName, type.AssemblyQualifiedName)
        {
            InitializationMode = InitializationMode.OnDemand,
            Ref = type.Assembly.CodeBase,
        };
    }
}

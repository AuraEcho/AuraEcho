using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Ioc;
using Prism.Modularity;

namespace AuraEcho.Services;

public class PluginManager : IPluginManager
{
    private bool _isInitialized;
    private readonly IModuleManager _moduleManager;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly ILocalPluginRepository _pluginRepository;
    private readonly IAppLogger _logger;
    private readonly IClientSession _clientSession;
    private readonly IContainerProvider _containerProvider;
    private readonly List<PluginLoadContext> _pluginLoadContexts = [];

    private List<UserPluginModel> _plugins;
    public List<UserPluginModel> Plugins
    {
        get => _isInitialized ? _plugins : [];
    }

    public PluginManager(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _clientSession = _containerProvider.Resolve<IClientSession>();
        _moduleManager = _containerProvider.Resolve<IModuleManager>();
        _moduleCatalog = _containerProvider.Resolve<IModuleCatalog>();
        _pluginRepository = _containerProvider.Resolve<ILocalPluginRepository>();
        _logger = _containerProvider.Resolve<IAppLogger>();
    }

    /// <summary>
    /// 加载所有插件并返回插件信息
    /// </summary>
    /// <returns></returns>
    public async Task<List<UserPluginModel>> LoadPluginsAsync()
    {
        // TODO: 线程安全

        if (_isInitialized)
        {
            _logger.Debug("插件管理器已初始化，跳过加载。");
            return _plugins;
        }

        _plugins = [];
        foreach (var pluginRegistry in await _pluginRepository.GetUserPluginsAsync(_clientSession.CurrentUser.Id))
        {
            await LoadPluginAsync(pluginRegistry);
        }
        _logger.Debug($"已加载 {_plugins.Count} 个插件。");

        _isInitialized = true;
        return _plugins;
    }

    public async Task<bool> LoadPluginAsync(UserPluginModel pluginRegistryModel)
    {
        if (pluginRegistryModel.Status == PluginPlanStatus.UninstallPending)
        {
            _pluginRepository.RemoveUserPluginAsync(pluginRegistryModel.Id);
            _logger.Debug($"插件 {pluginRegistryModel.LocalPlugin.Manifest.PluginName} 已被卸载，跳过加载。");
            return false;
        }

        string entryAssemblyPath = Path.Combine(
            pluginRegistryModel.LocalPlugin.PluginFolder,
            pluginRegistryModel.LocalPlugin.Manifest.EntryAssemblyName);

        if (!File.Exists(entryAssemblyPath))
        {
            _logger.Error($"插件 {pluginRegistryModel.LocalPlugin.Manifest.PluginName} 主程序集不存在：{entryAssemblyPath}");
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
            _logger.Error($"加载插件程序集失败：{pluginRegistryModel.LocalPlugin.Manifest.PluginName}，异常：{ex.Message}");
            return false;
        }

        IPlugin pluginContext = LoadPluginByAssembly(pluginAssembly);
        pluginRegistryModel.PluginContext = pluginContext;

        _logger.Debug("执行插件环境初始化");

        if (!pluginRegistryModel.LocalPlugin.IsSetup)
        {
            await pluginContext.SetupAsync(_containerProvider);

            pluginRegistryModel.LocalPlugin.IsSetup = true;
            _pluginRepository.UpdateLocalPluginAsync(pluginRegistryModel.LocalPlugin);
        }

        _plugins.Add(pluginRegistryModel);
        return true;
    }

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

    /// <summary>
    /// 清理旧版本插件
    /// </summary>
    /// <returns></returns>
    public async Task CleanOldPluginsAsync()
    {
        List<LocalPluginModel> plugins = await _pluginRepository.GetLocalPluginsAsync();

        await Task.Run(() => plugins.ForEach(CleanOldPlugin));

        void CleanOldPlugin(LocalPluginModel plugin)
        {
            DirectoryInfo? currentPluginRootPath = Directory.GetParent(plugin.PluginFolder);
            currentPluginRootPath.GetDirectories()
                                 .Where(d => !DirectoryUtils.AreDirectoriesEqual(d.FullName, plugin.PluginFolder))
                                 .Where(d => Version.TryParse(d.Name, out var v))
                                 .ForEach(DeleteDirectorySafely);
        }

        void DeleteDirectorySafely(DirectoryInfo dir)
        {
            try
            {
                dir.Delete(true);
                _logger.Information($"已成功清理旧版本目录: {dir.Name}");
            }
            catch (IOException)
            {
                _logger.Warning($"目录 {dir.Name} 正被占用，跳过本次清理。");
            }
        }
    }
}

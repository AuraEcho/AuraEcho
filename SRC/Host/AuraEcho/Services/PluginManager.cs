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

namespace AuraEcho.Services;

public class PluginManager : IPluginManager
{
    private bool _isInitialized;
    private readonly ILocalPluginRepository _pluginRepository;
    private readonly IAppLogger _logger;
    private readonly IClientSession _clientSession;
    private readonly IPluginLoader _pluginLoader;
    private readonly IContainerProvider _containerProvider;

    private List<AppPlugin> _plugins;
    public List<AppPlugin> Plugins
    {
        get => _isInitialized ? _plugins : [];
    }

    public PluginManager(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _clientSession = _containerProvider.Resolve<IClientSession>();
        _pluginRepository = _containerProvider.Resolve<ILocalPluginRepository>();
        _logger = _containerProvider.Resolve<IAppLogger>();
        _pluginLoader = _containerProvider.Resolve<IPluginLoader>();
    }

    /// <summary>
    /// 加载所有插件并返回插件信息
    /// </summary>
    /// <returns></returns>
    public async Task<List<AppPlugin>> LoadPluginsAsync()
    {
        // TODO: 线程安全
        // TODO: 使用异步流模式？

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

    public async Task<AppPlugin> LoadPluginAsync(UserPluginModel pluginRegistryModel)
    {
        try
        {
            if (pluginRegistryModel.Status == PluginPlanStatus.UninstallPending)
            {
                await _pluginRepository.RemoveUserPluginAsync(pluginRegistryModel.Id);
                _logger.Debug($"插件 {pluginRegistryModel.LocalPlugin.PluginId} 已被卸载，跳过加载。");
                return null;
            }

            var plugin = await _pluginLoader.LoadPluginAsync(pluginRegistryModel);

            _plugins.Add(plugin);
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载插件 {pluginRegistryModel.LocalPlugin.PluginId} 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清理旧版本插件
    /// </summary>
    /// <returns></returns>
    public async Task CleanOldPluginsAsync()
    {
        List<InstalledPluginModel> plugins = await _pluginRepository.GetLocalPluginsAsync();

        await Task.Run(() => plugins.ForEach(CleanOldPlugin));

        void CleanOldPlugin(InstalledPluginModel plugin)
        {
            DirectoryInfo? currentPluginRootPath = Directory.GetParent(plugin.InstallPath);
            currentPluginRootPath.GetDirectories()
                                 .Where(d => !DirectoryUtils.AreDirectoriesEqual(d.FullName, plugin.InstallPath))
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

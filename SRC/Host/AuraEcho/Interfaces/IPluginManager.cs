using System.Collections.Generic;
using System.Threading.Tasks;
using AuraEcho.Core.Models;

namespace AuraEcho.Interfaces;

public interface IPluginManager
{
    List<AppPlugin> Plugins { get; }

    //List<UserPluginModel> LoadPlugins();
    Task<AppPlugin> LoadPluginAsync(UserPluginModel pluginRegistryModel);
    /// <summary>
    /// 加载所有插件并返回插件注册表
    /// </summary>
    Task<List<AppPlugin>> LoadPluginsAsync();

    /// <summary>
    /// 清理旧版本插件
    /// </summary>
    /// <returns></returns>
    Task CleanOldPluginsAsync();
}

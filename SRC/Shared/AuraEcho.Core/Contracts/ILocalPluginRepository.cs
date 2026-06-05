using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface ILocalPluginRepository
{
    /// <summary>
    /// 获取插件信息列表
    /// </summary>
    /// <returns></returns>
    Task<List<InstalledPluginModel>> GetLocalPluginsAsync();

    /// <summary>
    /// 添加插件信息
    /// </summary>
    /// <param name="pluginRegistryModel"></param>
    Task AddLocalPluginAsync(InstalledPluginModel newPlugin);

    /// <summary>
    /// 移除插件信息
    /// </summary>
    /// <param name="localPluginId"></param>
    Task RemoveLocalPluginAsync(Guid localPluginId);

    /// <summary>
    /// 更新插件信息
    /// </summary>
    /// <param name="plugin"></param>
    Task UpdateLocalPluginAsync(InstalledPluginModel plugin);

    /// <summary>
    /// 获取用户插件列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<UserPluginModel>> GetUserPluginsAsync(Guid userId);

    /// <summary>
    /// 删除用户插件
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="localPluginId"></param>
    Task RemoveUserPluginAsync(Guid userId, Guid localPluginId);

    /// <summary>
    /// 删除用户插件
    /// </summary>
    Task RemoveUserPluginAsync(Guid userPluginId);

    /// <summary>
    /// 添加用户插件
    /// </summary>
    /// <param name="plugin"></param>
    Task<UserPluginModel> AddUserPluginAsync(Guid userId, Guid localPluginId);

    Task<UserPluginModel> GetUserPluginAsync(Guid userPluginId);

    /// <summary>
    /// 更新用户插件状态
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="localPluginId"></param>
    /// <param name="newStatus"></param>
    Task UpdateUserPluginStatusAsync(Guid userId, Guid localPluginId, PluginPlanStatus newStatus);
}

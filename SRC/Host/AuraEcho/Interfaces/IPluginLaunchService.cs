using System;
using AuraEcho.Core.Models;

namespace AuraEcho.Interfaces;

/// <summary>
/// 插件启动服务
/// </summary>
public interface IPluginLaunchService
{
    /// <summary>
    /// 打开指定插件
    /// </summary>
    /// <param name="plugin">待打开的插件</param>
    void Launch(AppPlugin plugin);

    /// <summary>
    /// 按插件 Id 打开插件
    /// </summary>
    /// <param name="pluginId">插件 Id</param>
    /// <returns>插件是否已成功打开</returns>
    bool Launch(Guid pluginId);
}

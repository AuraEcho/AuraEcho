using AuraEcho.Core.Models;

namespace AuraEcho.Interfaces;

/// <summary>
/// 系统级 Toast 通知服务
/// </summary>
public interface ISystemToastService
{
    /// <summary>
    /// 程序当前是否位于前台
    /// </summary>
    bool IsAppInForeground { get; }

    /// <summary>
    /// 推送插件安装完成通知
    /// </summary>
    /// <param name="plugin">已安装完成的插件</param>
    void NotifyPluginInstalled(AppPlugin plugin);
}

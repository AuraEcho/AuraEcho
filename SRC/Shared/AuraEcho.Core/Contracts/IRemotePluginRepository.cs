using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IRemotePluginRepository
{
    Task<List<RemotePlugin>> GetPluginsAsync();
    Task<List<RemotePlugin>> GetAllPluginsAsync();
    Task<PluginPackage> GetLatestAsync(Guid pluginId);
    Task<bool> AcquireAsync(Guid pluginId);
    Task<List<PluginScreenshot>> GetScreenshotsAsync(Guid pluginId);
}

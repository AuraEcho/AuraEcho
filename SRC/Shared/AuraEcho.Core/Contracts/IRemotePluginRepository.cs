using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;

namespace AuraEcho.Core.Contracts;

public interface IRemotePluginRepository
{
    Task<List<RemotePlugin>> GetPluginsAsync();
    Task<List<RemotePlugin>> GetAllPluginsAsync();
    Task<Guid?> CreatePluginAsync(CreatePluginRequest req);
    Task<Guid?> CreateVersionAsync(CreatePluginVersionRequest req);
    Task<List<PluginPackage>> GetVersionsAsync(Guid pluginId);
    Task<PluginPackage> GetLatestAsync(Guid pluginId);
    Task<bool> DownloadLatestAsync(Guid pluginId, string build, string outputPath, IProgress<double> progress);
    Task<bool> DeleteAsync(Guid pluginId);
    Task<bool> DeleteVersionAsync(Guid versionId);
    Task<bool> AcquireAsync(Guid pluginId);
    Task<List<PluginScreenshot>> GetScreenshotsAsync(Guid pluginId);
}

using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IPluginInstallService
{
    public Task<InstalledPluginModel> InstallAsync(string filePath);
}

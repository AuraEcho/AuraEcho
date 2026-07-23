using AuraEcho.Domain;

namespace AuraEcho.Core.Contracts;

public interface IPluginInstallService
{
    public Task<InstalledPluginModel> InstallAsync(string filePath);
}

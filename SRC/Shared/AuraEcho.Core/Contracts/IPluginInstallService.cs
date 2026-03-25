using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IPluginInstallService
{
    public Task<LocalPluginModel> InstallAsync(string filePath);
}

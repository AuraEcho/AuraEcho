using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IAppPackageRepository
{
    Task<AppVersionInfo> GetLatestAsync();
}

using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface IAppPackageRepository
{
    Task<Guid?> CreatePackageAsync(Guid fullFileId, Guid updateFileId, string name, string version);
    Task<List<AppPackageDetail>> GetUploadedPackagesAsync();
    Task<AppVersionInfo> GetLatestAsync();
    Task<bool> DownloadLatestAsync(bool isFull, string outputPath, IProgress<double> progress);
    Task<bool> DeleteAsync(Guid packageId);
}

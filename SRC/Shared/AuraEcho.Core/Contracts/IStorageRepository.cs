using AuraEcho.ClientApi.V1.File;

namespace AuraEcho.Core.Contracts;

public interface IStorageRepository
{
    Task<bool> DownloadFileAsync(string url, string outputPath, IProgress<double> progress);
    Task<UploadFileResponse> UploadFileAsync(string filePath, IProgress<double> progress = null);
}
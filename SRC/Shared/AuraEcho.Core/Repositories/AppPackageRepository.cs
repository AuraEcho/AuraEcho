using System.IO;
using System.Net.Http;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class AppPackageRepository : IAppPackageRepository
{
    private readonly HttpHelper _httpHelper;
    public AppPackageRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }
    public async Task<Guid?> CreatePackageAsync(Guid fullFileId, Guid updateFileId, string name, string version)
    {
        var request = new CreatePackageRequest { Name = name, Version = version, FullFileId = fullFileId, UpdateFileId = updateFileId };
        var response = await _httpHelper.PostAsync<CreatePackageResponse>($"{Urls.ServerUrl}/api/v1/package/create", request);
        return response?.PackageId;
    }

    public async Task<bool> DeleteAsync(Guid packageId)
    {
        var resp = await _httpHelper.DeleteAsync($"{Urls.ServerUrl}/api/v1/package/delete/{packageId}");
        return resp;
    }

    public async Task<bool> DownloadLatestAsync(bool isFull, string outputPath, IProgress<double> progress)
    {
        try
        {
            using var response = await _httpHelper.GetAsync($"{Urls.ServerUrl}/api/v1/package/download?isFull={isFull}", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[80 * 1024];
            long totalRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;

                if (totalBytes > 0)
                {
                    double percent = totalRead * 100.0 / totalBytes;
                    progress?.Report(percent);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AppVersionInfo> GetLatestAsync()
    {
        var result = await _httpHelper.GetAsync<GetLatestVersionResponse>($"{Urls.ServerUrl}/api/v1/package/latest");
        if (result is null) return null;

        return new AppVersionInfo
        {
            Version = result.Version,
            FullFileId = result.FullFileId,
            FullFileName = result.FullFileName,
            FullFileSize = result.FullFileSize,
            UpdateFileId = result.UpdateFileId,
            UpdateFileName = result.UpdateFileName,
            UpdateFileSize = result.UpdateFileSize,
            ReleaseDate = result.ReleaseDate,
        };
    }

    public async Task<List<AppPackageDetail>> GetUploadedPackagesAsync()
    {
        var result = await _httpHelper.GetAsync<ListAllPackagesResponse>($"{Urls.ServerUrl}/api/v1/package/listAll");
        if (result is null) return null;

        List<AppPackageDetail> packages =
            result.Packages.Select(p => new AppPackageDetail
            {
                IsActive = p.IsActive,
                CreateTime = p.CreateTime,
                FullFileId = p.FullFileId,
                FullFileName = p.FullFileName,
                FullFileSize = p.FullFileSize,
                UpdateFileId = p.UpdateFileId,
                UpdateFileName = p.UpdateFileName,
                UpdateFileSize = p.UpdateFileSize,
                Id = p.Id,
                Name = p.Name,
                Version = p.Version,
            }).ToList();
        return packages;
    }
}


using AuraEcho.ClientApi.V1.AppPackage;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class AppPackageRepository : IAppPackageRepository
{
    private readonly HttpHelper _httpHelper;
    public AppPackageRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }
    public async Task<AppVersionInfo> GetLatestAsync()
    {
        var result = await _httpHelper.GetAsync<GetLatestVersionResponse>(Urls.GetLatestPackageVersion());
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
}


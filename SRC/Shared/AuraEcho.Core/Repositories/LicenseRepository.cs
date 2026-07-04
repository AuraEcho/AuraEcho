using AuraEcho.ClientApi.V1.Common;
using AuraEcho.ClientApi.V1.License;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly HttpHelper _httpHelper;
    public LicenseRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }
    public async Task<ResourceLicense> GetResourceLicenseAsync(Guid resourceId)
    {
        var result = await _httpHelper.GetAsync<LicenseResponseItem>(Urls.GetResourceLicense(resourceId));
        if (result is null) return null;

        var license = new ResourceLicense
        {
            ExpiredAt = result.ExpiredAt,
            IsValid = result.IsValid,
            LicenseType = (PluginContracts.Models.LicenseType)(int)result.LicenseType
        };
        return license;
    }

    public async Task<List<ResourceLicense>> GetUserLicensesAsync()
    {
        var result = await _httpHelper.GetAsync<ResponseResult<List<LicenseResponseItem>>>(Urls.GetUserLicenses());
        if (result is null || result.Status != ResultStatus.Success) return [];

        var licenses = result.Data.Select(item => new ResourceLicense
        {
            ExpiredAt = item.ExpiredAt,
            IsValid = item.IsValid,
            LicenseType = (PluginContracts.Models.LicenseType)(int)item.LicenseType
        }).ToList();
        return licenses;
    }
}

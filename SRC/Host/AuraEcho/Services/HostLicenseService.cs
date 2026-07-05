using System;
using System.Threading.Tasks;
using AuraEcho.Cloud.V1;
using AuraEcho.Core.Extensions;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Services;

public class HostLicenseService : ILicenseService
{
    private readonly ApiClient _apiClient;

    public HostLicenseService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ResourceLicense> GetResourceLicenseAsync(Guid resourceId)
    {
        var result = await _apiClient.License.GetResourceLicenseAsync(resourceId);
        return result?.ToResourceLicense();
    }
}

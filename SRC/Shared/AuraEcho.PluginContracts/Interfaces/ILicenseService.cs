using System;
using System.Threading.Tasks;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface ILicenseService
    {
        Task<ResourceLicense> GetResourceLicenseAsync(Guid resourceId);
    }
}
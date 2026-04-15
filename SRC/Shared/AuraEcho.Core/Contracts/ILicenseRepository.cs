using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Contracts;

public interface ILicenseRepository : ILicenseService
{
    Task<List<ResourceLicense>> GetUserLicensesAsync();
}

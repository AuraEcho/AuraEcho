using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models.Api;

public class LicenseResponseItem
{
    public bool IsValid { get; set; }
    public LicenseType LicenseType { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

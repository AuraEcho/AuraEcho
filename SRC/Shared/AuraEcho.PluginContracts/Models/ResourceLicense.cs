using System;
namespace AuraEcho.PluginContracts.Models
{
    public class ResourceLicense
    {
        public bool IsValid { get; set; }
        public LicenseType LicenseType { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}

using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Models;

public class HostSettings
{
    public AppTheme AppTheme { get; set; }
    public AppLanguage AppLanguage { get; set; }
    public bool HardwareAcceleration { get; set; }
    public bool TelemetryEnabled { get; set; } = true;
    public static HostSettings Default => new()
    {
        AppTheme = AppTheme.Dark,
        AppLanguage = AppLanguage.ChineseSimplified,
        HardwareAcceleration = true,
        TelemetryEnabled = true
    };
}

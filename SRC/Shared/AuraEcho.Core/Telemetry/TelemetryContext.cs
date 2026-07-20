namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 遥测上下文信息
/// </summary>
public class TelemetryContext
{
    public Guid InstallationId { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public string NetVersion { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
    public string CpuModel { get; init; } = string.Empty;
    public string GpuModel { get; init; } = string.Empty;
    public string ScreenResolution { get; init; } = string.Empty;
    public int ScreenDpi { get; init; }
    public string NetworkType { get; init; } = string.Empty;

    /// <summary>
    /// 将上下文信息展开为用于遥测上报的 Properties 字典。
    /// </summary>
    public Dictionary<string, string> ToProperties()
    {
        return new Dictionary<string, string>
        {
            ["installationId"] = InstallationId.ToString(),
            ["appVersion"] = AppVersion,
            ["osVersion"] = OsVersion,
            ["netVersion"] = NetVersion,
            ["culture"] = Culture,
            ["cpuModel"] = CpuModel,
            ["gpuModel"] = GpuModel,
            ["screenResolution"] = ScreenResolution,
            ["screenDpi"] = ScreenDpi.ToString(),
            ["networkType"] = NetworkType
        };
    }
}

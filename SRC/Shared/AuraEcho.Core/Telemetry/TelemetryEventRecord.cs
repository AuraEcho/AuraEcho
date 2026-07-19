using AuraEcho.Cloud.V1.Models.Telemetry;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 遥测事件信息
/// </summary>
public class TelemetryEventRecord
{
    public Guid Id { get; init; }

    public DateTime Timestamp { get; init; }

    public TelemetryEventType Type { get; init; }

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 客户端区域信息
    /// </summary>
    public string Culture { get; init; } = string.Empty;

    public Dictionary<string, string>? Properties { get; init; }

    public Dictionary<string, double>? Metrics { get; init; }

    /// <summary>
    /// 客户端安装标识
    /// </summary>
    public Guid InstallationId { get; init; }

    /// <summary>
    /// 客户端版本
    /// </summary>
    public string AppVersion { get; init; } = string.Empty;

    /// <summary>
    /// 操作系统版本
    /// </summary>
    public string OSVersion { get; init; } = string.Empty;

    /// <summary>
    /// .NET 运行时版本
    /// </summary>
    public string NetVersion { get; init; } = string.Empty;

    /// <summary>
    /// 客户端会话标识
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// CPU 型号名称。
    /// </summary>
    public string CpuModel { get; init; } = string.Empty;

    /// <summary>
    /// CPU 逻辑核心数。
    /// </summary>
    public int CpuCoreCount { get; init; }

    /// <summary>
    /// 显卡型号名称。
    /// </summary>
    public string GpuModel { get; init; } = string.Empty;

    /// <summary>
    /// 主屏分辨率
    /// </summary>
    public string ScreenResolution { get; init; } = string.Empty;

    /// <summary>
    /// 主屏 DPI。
    /// </summary>
    public int ScreenDpi { get; init; }
}

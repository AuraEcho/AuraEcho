using AuraEcho.Cloud.V1.Models.Telemetry;

namespace AuraEcho.Core.Data.Entities;

/// <summary>
/// 遥测事件本地缓存实体。
/// </summary>
public class TelemetryEventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Timestamp { get; set; }

    public TelemetryEventType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 客户端安装标识
    /// </summary>
    public Guid InstallationId { get; set; }

    /// <summary>
    /// 客户端版本
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 操作系统版本
    /// </summary>
    public string OSVersion { get; set; } = string.Empty;

    /// <summary>
    /// .NET 运行时版本
    /// </summary>
    public string NetVersion { get; set; } = string.Empty;

    /// <summary>
    /// 客户端会话标识
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 客户端区域信息
    /// </summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// CPU 型号名称。
    /// </summary>
    public string CpuModel { get; set; } = string.Empty;

    /// <summary>
    /// CPU 逻辑核心数。
    /// </summary>
    public int CpuCoreCount { get; set; }

    /// <summary>
    /// 显卡型号名称。
    /// </summary>
    public string GpuModel { get; set; } = string.Empty;

    /// <summary>
    /// 主屏分辨率（如 "1920x1080"）。
    /// </summary>
    public string ScreenResolution { get; set; } = string.Empty;

    /// <summary>
    /// 主屏 DPI。
    /// </summary>
    public int ScreenDpi { get; set; }

    /// <summary>
    /// 附加属性
    /// </summary>
    public Dictionary<string, string>? Properties { get; set; }

    /// <summary>
    /// 数值指标
    /// </summary>
    public Dictionary<string, double>? Metrics { get; set; }

    public DateTime CreatedAt { get; set; }
}

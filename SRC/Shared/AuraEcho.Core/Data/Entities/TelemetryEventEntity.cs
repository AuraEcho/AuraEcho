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
    /// 客户端会话标识，关联到 Sessions 表的上下文信息。
    /// </summary>
    public Guid SessionId { get; set; }

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

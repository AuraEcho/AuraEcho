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

    public Dictionary<string, string>? Properties { get; init; }

    public Dictionary<string, double>? Metrics { get; init; }

    /// <summary>
    /// 客户端会话标识，关联到 Sessions 表的上下文信息。
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// 会话内单调递增的事件序号，用于确定性还原操作顺序。
    /// </summary>
    public long SequenceNumber { get; init; }
}

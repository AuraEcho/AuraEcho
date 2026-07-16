namespace AuraEcho.Core.Data.Entities;

/// <summary>
/// 遥测事件本地缓存实体。
/// </summary>
public class TelemetryEventEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    public DateTime Timestamp { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 附加属性
    /// </summary>
    public Dictionary<string, string>? Properties { get; set; }

    /// <summary>
    /// 数值指标
    /// </summary>
    public Dictionary<string, double>? Metrics { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

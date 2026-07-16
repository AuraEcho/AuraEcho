using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 本地遥测数据缓存
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryStore _store;

    public TelemetryService(TelemetryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public bool IsEnabled { get; set; } = true;

    public void TrackEvent(string name, Dictionary<string, string> properties = null)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.Event, name, properties, null);
    }

    public void TrackMetric(string name, double value, Dictionary<string, string> properties = null)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.Metric, name, properties, new Dictionary<string, double> { ["value"] = value });
    }

    public void TrackException(Exception exception, Dictionary<string, string> properties = null)
    {
        if (!IsEnabled) return;
        var props = new Dictionary<string, string>(properties ?? new Dictionary<string, string>())
        {
            ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name,
            ["exceptionMessage"] = exception.Message,
            ["exceptionStackTrace"] = exception.StackTrace ?? string.Empty
        };

        Enqueue(TelemetryEventType.Exception, exception.GetType().Name, props, null);
    }

    public void TrackPageView(string pageName)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.PageView, pageName, null, null);
    }

    private void Enqueue(
        TelemetryEventType type,
        string name,
        Dictionary<string, string>? properties,
        Dictionary<string, double>? metrics)
    {
        try
        {
            var evt = new TelemetryEvent
            {
                Type = type,
                Name = name,
                Properties = properties,
                Metrics = metrics
            };

            _store.Enqueue(evt);
        }
        catch
        {
            // 遥测写入失败不应影响主业务逻辑
        }
    }
}

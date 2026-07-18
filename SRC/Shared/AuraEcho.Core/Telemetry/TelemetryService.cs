using System.Globalization;
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

        // 异常消息的最大长度
        const int MAX_EXCEPTION_MESSAGE_LENGTH = 2000;

        // 异常堆栈的最大长度
        const int MAX_STACK_TRACE_LENGTH = 8000;

        // 内部异常的层数上限
        const int MAX_INNER_EXCEPTION_DEPTH = 16;

        var props = new Dictionary<string, string>(properties ?? new Dictionary<string, string>())
        {
            ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name,
            ["exceptionMessage"] = Truncate(exception.Message, MAX_EXCEPTION_MESSAGE_LENGTH),
            ["exceptionStackTrace"] = Truncate(exception.StackTrace ?? string.Empty, MAX_STACK_TRACE_LENGTH)
        };

        // 内部异常信息
        var seen = new HashSet<Exception>(ReferenceEqualityComparer.Instance) { exception };
        var inner = exception.InnerException;
        var depth = 0;
        while (inner is not null && depth < MAX_INNER_EXCEPTION_DEPTH && seen.Add(inner))
        {
            var prefix = $"innerException{depth}";
            props[$"{prefix}Type"] = inner.GetType().FullName ?? inner.GetType().Name;
            props[$"{prefix}Message"] = Truncate(inner.Message, MAX_EXCEPTION_MESSAGE_LENGTH);
            props[$"{prefix}StackTrace"] = Truncate(inner.StackTrace ?? string.Empty, MAX_STACK_TRACE_LENGTH);

            inner = inner.InnerException;
            depth++;
        }

        // 根异常
        var baseException = exception.GetBaseException();
        if (!ReferenceEquals(baseException, exception))
        {
            props["baseExceptionType"] = baseException.GetType().FullName ?? baseException.GetType().Name;
            props["baseExceptionMessage"] = Truncate(baseException.Message, MAX_EXCEPTION_MESSAGE_LENGTH);
        }

        Enqueue(TelemetryEventType.Exception, exception.GetType().Name, props, null);
    }

    /// <summary>
    /// 将字符串截断到指定长度，超出部分以省略标记结尾
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        const string ellipsis = "...[truncated]";
        return string.Concat(value.AsSpan(0, maxLength), ellipsis);
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
                Culture = CultureInfo.CurrentCulture.Name,
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

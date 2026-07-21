using System.Threading.Channels;
using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.Toolkit;

namespace AuraEcho.Telemetry;

/// <summary>
/// 本地遥测数据缓存
/// </summary>
public class TelemetryService : ITelemetryService
{
    // 单次落库事件数上限
    private const int MAX_DRAIN_PER_WAKE = 50;

    private readonly TelemetryStore _store;
    private readonly TelemetryContextFactory _contextFactory;
    private readonly Channel<TelemetryEvent> _channel;
    private readonly Task _flushTask;

    /// <summary>
    /// 会话内单调递增的事件序号。
    /// </summary>
    private long _sequence;

    // 入队限流器
    private readonly TokenBucket _rateLimiter = new(3, 50);

    public TelemetryService(TelemetryStore store, TelemetryContextFactory contextFactory)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _channel = Channel.CreateBounded<TelemetryEvent>(new BoundedChannelOptions(50)
        {
            // 满时丢旧保新
            FullMode = BoundedChannelFullMode.DropOldest,
            // 启用无锁快路径
            SingleReader = true,
            // 多线程并发写入安全
            SingleWriter = false,
            // 防止 await 续执在调用线程
            AllowSynchronousContinuations = false
        });

        _flushTask = Task.Run(FlushLoopAsync);
    }

    public bool IsEnabled { get; set; } = true;


    public void TrackEvent(string name, Dictionary<string, string>? properties = null)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.Event, name, properties, null);
    }

    public void TrackMetric(string name, Dictionary<string, double> metrics, Dictionary<string, string>? properties = null)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.Metric, name, properties, metrics);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
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

    public void TrackPageView(string pageName, Dictionary<string, string>? properties = null)
    {
        if (!IsEnabled) return;
        Enqueue(TelemetryEventType.PageView, pageName, properties, null);
    }

    /// <summary>
    /// 停止后台缓存任务
    /// </summary>
    public async Task FlushAndShutdownAsync(TimeSpan timeout)
    {
        try
        {
            _channel.Writer.Complete();

            using var cts = new CancellationTokenSource(timeout);
            await _flushTask.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception) { }
    }

    private async Task FlushLoopAsync()
    {
        var batch = new List<TelemetryEvent>(MAX_DRAIN_PER_WAKE);

        while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (batch.Count < MAX_DRAIN_PER_WAKE && _channel.Reader.TryRead(out var evt))
                batch.Add(evt);

            FlushBatch(batch);
        }
    }

    private void FlushBatch(List<TelemetryEvent> batch)
    {
        if (batch.Count == 0) return;
        try
        {
            _store.EnqueueBatch(batch);
        }
        catch
        {
            // 遥测落库失败不应影响主业务逻辑
        }
        finally
        {
            batch.Clear();
        }
    }

    private void Enqueue(
        TelemetryEventType type,
        string name,
        Dictionary<string, string>? properties,
        Dictionary<string, double>? metrics)
    {
        try
        {
            if (!_rateLimiter.TryAcquire())
                return;

            var evt = new TelemetryEvent
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Type = type,
                Name = name,
                SessionId = _contextFactory.SessionId,
                SequenceNumber = Interlocked.Increment(ref _sequence),
                Properties = properties,
                Metrics = metrics
            };

            _channel.Writer.TryWrite(evt);
        }
        catch
        {
            // 遥测入队失败不应影响主业务逻辑
        }
    }

    /// <summary>
    /// 将字符串截断到指定长度，超出部分以省略标记结尾
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        const string ellipsis = "...[truncated]";
        return string.Concat(value.AsSpan(0, maxLength), ellipsis);
    }
}

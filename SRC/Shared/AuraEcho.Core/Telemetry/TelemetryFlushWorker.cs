using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.Core.Telemetry;

public class TelemetryFlushWorker : IDisposable
{
    private readonly TelemetryBuffer _buffer;
    private readonly TelemetryContextFactory _contextFactory;
    private readonly ApiClient _apiClient;
    private readonly IAppLogger _logger;
    private readonly TelemetryFlushOptions _options;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public TelemetryFlushWorker(
        TelemetryBuffer buffer,
        TelemetryContextFactory contextFactory,
        ApiClient apiClient,
        IAppLogger logger)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
        _options = TelemetryFlushOptions.Default;
    }

    public void Start()
    {
        if (_timer != null) return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_options.FlushInterval);
        _loopTask = RunLoopAsync(_cts.Token);

        _logger.Debug("遥测后台刷新已启动");
    }

    public async Task StopAsync()
    {
        if (_timer == null) return;

        _timer.Dispose();
        _timer = null;

        _cts?.Cancel();

        if (_loopTask != null)
        {
            try { await _loopTask; }
            catch (OperationCanceledException) { }
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct))
            {
                await FlushOnceAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    private async Task FlushOnceAsync()
    {
        try
        {
            // 清理死信事件
            _buffer.PurgeDeadEvents(_options.MaxRetries);

            var events = _buffer.Dequeue(_options.BatchSize);
            if (events.Count == 0) return;

            var context = _contextFactory.Context;

            var batch = new TelemetryBatch
            {
                Context = context,
                Events = events,
                SentAt = DateTime.UtcNow
            };

            var success = await _apiClient.Telemetry.SendBatchAsync(batch);

            if (success)
            {
                _buffer.Delete(events.Select(e => e.Id));
                _logger?.Debug($"遥测批量发送成功: {events.Count} 条事件");
            }
            else
            {
                _buffer.IncrementRetryCount(events.Select(e => e.Id));
                _logger?.Warning("遥测批量发送失败，已标记重试");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"遥测刷新过程中发生异常: {ex}");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cts?.Dispose();
    }

    /// <summary>
    /// 遥测配置选项。
    /// </summary>
    private class TelemetryFlushOptions
    {
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
        public int BatchSize { get; set; } = 50;
        public int MaxRetries { get; set; } = 5;

        public static TelemetryFlushOptions Default => new();
    }
}

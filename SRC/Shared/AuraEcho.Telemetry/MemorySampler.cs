using System.Diagnostics;

namespace AuraEcho.Telemetry;

/// <summary>
/// 运行时内存定时采样器
/// </summary>
public class MemorySampler
{
    private const int SAMPLE_INTERVAL_MINUTES = 10;
    private const int STARTUP_DELAY_SECONDS = 20;

    private readonly ITelemetryService _telemetry;
    private readonly CancellationTokenSource _cts = new();
    private PerformanceCounter? _privateWorkingSetCounter;
    private Process _currentProcess;
    private Task? _loopTask;

    public MemorySampler(ITelemetryService telemetry)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    /// <summary>
    /// 创建 专用工作集性能计数器。
    /// </summary>
    private static PerformanceCounter? TryCreatePrivateWorkingSetCounter(Process targetProcess)
    {
        try
        {
            return new PerformanceCounter("Process", "Working Set - Private", targetProcess.ProcessName, true);
        }
        catch
        {
            // 权限不足或性能计数器损坏时静默失败
            return null;
        }
    }

    public void Start()
    {
        if (_loopTask is not null) return;
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public async Task StopAsync(TimeSpan timeout)
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException) { }

        if (_loopTask is not null)
        {
            using var cts = new CancellationTokenSource(timeout);
            await _loopTask.WaitAsync(cts.Token).ConfigureAwait(false);
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            _currentProcess = Process.GetCurrentProcess();
            // 初始化性能计数器
            _privateWorkingSetCounter = TryCreatePrivateWorkingSetCounter(_currentProcess);
            _privateWorkingSetCounter.NextValue();

            await Task.Delay(TimeSpan.FromSeconds(STARTUP_DELAY_SECONDS), ct);
            Sample(ct);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(SAMPLE_INTERVAL_MINUTES));
            while (await timer.WaitForNextTickAsync(ct))
            {
                Sample(ct);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private void Sample(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            // 总工作集
            var workingSetMB = Math.Round(_currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2);
            // 峰值工作集
            var peakWorkingSetMB = Math.Round(_currentProcess.PeakWorkingSet64 / (1024.0 * 1024.0), 2);
            // 专用工作集
            var privateWS = ReadPrivateWorkingSetMB();

            var metrics = new Dictionary<string, double>
            {
                ["workingSetMB"] = workingSetMB,
                ["peakWorkingSetMB"] = peakWorkingSetMB,
                ["privateWS_MB"] = privateWS ?? 0D
            };

            _telemetry.TrackMetric("Memory.WorkingSet", metrics);
        }
        catch
        {
            // 采样失败不应影响主流程
        }
    }

    private double? ReadPrivateWorkingSetMB()
    {
        if (_privateWorkingSetCounter is null) return null;

        try
        {
            return Math.Round(_privateWorkingSetCounter.NextValue() / (1024.0 * 1024.0), 2);
        }
        catch
        {
            return null;
        }
    }
}

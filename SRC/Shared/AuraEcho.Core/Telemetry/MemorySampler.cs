using System.Collections.Generic;
using System.Diagnostics;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 运行时内存定时采样器
/// </summary>
public class MemorySampler
{
    private const int SAMPLE_INTERVAL_SECONDS = 60;
    private const int STARTUP_DELAY_SECONDS = 30;

    private readonly ITelemetryService _telemetry;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loopTask;

    public MemorySampler(ITelemetryService telemetry)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
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
            await Task.Delay(TimeSpan.FromSeconds(STARTUP_DELAY_SECONDS), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(SAMPLE_INTERVAL_SECONDS));

        while (await timer.WaitForNextTickAsync(ct))
        {
            Sample(ct);
        }
    }

    private void Sample(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            using var proc = Process.GetCurrentProcess();
            var workingSetMB = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2);
            var managedHeapMB = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);

            _telemetry.TrackMetric(
                "Memory.WorkingSetMB", workingSetMB,
                new Dictionary<string, string>
                {
                    ["managedHeapMB"] = managedHeapMB.ToString("F2")
                });
        }
        catch
        {
            // 采样失败不应影响主流程
        }
    }
}

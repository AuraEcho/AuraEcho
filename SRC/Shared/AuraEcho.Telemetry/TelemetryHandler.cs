using System.Diagnostics;
using System.Net.Http;

namespace AuraEcho.Telemetry;

/// <summary>
/// 遥测 HTTP 处理器 —— 统计请求耗时与状态码并上报遥测，不涉及日志输出。
/// 应与 <c>LoggingHandler</c> 配合使用放在其内侧，或独立使用。
/// </summary>
public sealed class TelemetryHandler : DelegatingHandler
{
    private readonly ITelemetryService _telemetry;

    public TelemetryHandler(ITelemetryService telemetry)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            TrackHttp(request, (int)response.StatusCode, sw.Elapsed, succeeded: response.IsSuccessStatusCode);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            TrackHttp(request, statusCode: 0, sw.Elapsed, succeeded: false, exceptionType: ex.GetType().Name);
            throw;
        }
    }

    private void TrackHttp(HttpRequestMessage request, int statusCode, TimeSpan elapsed, bool succeeded, string? exceptionType = null)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        // 排除遥测自身的上报请求，避免递归
        if (path.Contains("/telemetry", StringComparison.OrdinalIgnoreCase)) return;

        var props = new Dictionary<string, string>
        {
            ["path"] = path,
            ["method"] = request.Method.Method,
            ["statusCode"] = statusCode.ToString(),
            ["succeeded"] = succeeded ? "true" : "false"
        };
        if (exceptionType is not null)
            props["exceptionType"] = exceptionType;

        _telemetry.TrackMetric("Http.Duration", new Dictionary<string, double> { ["value"] = elapsed.TotalMilliseconds }, props);
    }
}

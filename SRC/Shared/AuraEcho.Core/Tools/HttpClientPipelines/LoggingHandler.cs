using AuraEcho.PluginContracts.Interfaces;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace AuraEcho.Core.Tools.HttpClientPipelines;

public sealed class LoggingHandler : DelegatingHandler
{
    private readonly IAppLogger _logger;
    private readonly ITelemetryService? _telemetry;

    public LoggingHandler(IAppLogger logger, ITelemetryService? telemetry = null)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString("N"));

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            var logText = await BuildLogString(request, response, sw.Elapsed, cancellationToken);
            _logger?.Debug(logText);

            TrackHttp(request, (int)response.StatusCode, sw.Elapsed, succeeded: response.IsSuccessStatusCode);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var logText = BuildErrorLogString(request, ex, sw.Elapsed);
            _logger?.Error(logText);

            TrackHttp(request, statusCode: 0, sw.Elapsed, succeeded: false, exceptionType: ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// 上报网络请求指标。
    /// </summary>
    private void TrackHttp(HttpRequestMessage request, int statusCode, TimeSpan elapsed, bool succeeded, string? exceptionType = null)
    {
        if (_telemetry is null) return;

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        // 排除遥测自身的上报请求，避免递归；其余请求全量上报以完整反映用户的网络行为
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

    private static async Task<string> BuildLogString(HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken ct)
    {
        var sb = new StringBuilder(8192);

        sb.AppendLine();
        sb.AppendLine("┌─────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"│ API Request: [{request.Method}] {request.RequestUri}");
        sb.AppendLine("├─────────────────────────────────────────────────────────────────────────────");

        // ========== Request ==========
        sb.AppendLine("├─ Request");
        foreach (var h in request.Headers)
            sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");

        if (request.Content != null)
        {
            foreach (var h in request.Content.Headers)
                sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");

            if (IsTextBasedContentType(request.Content))
            {
                var body = await request.Content.ReadAsStringAsync(ct);
                if (!String.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine("│  Body: " + body.Replace("\n", "\n│  "));
                }
            }
        }

        sb.AppendLine("│");

        // ========== Response ==========
        sb.AppendLine("├─ Response");
        sb.AppendLine($"│  HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

        foreach (var h in response.Headers)
            sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");

        if (response.Content != null)
        {
            foreach (var h in response.Content.Headers)
                sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");
            if (IsTextBasedContentType(response.Content))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!String.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine("│  Body: " + body.Replace("\n", "\n│  "));
                }
            }
        }

        sb.AppendLine($"└─────────────────────────────────Elapsed: {elapsed.TotalMilliseconds:0000} ms─────────────────────────────");

        return sb.ToString();
    }

    private static bool IsTextBasedContentType(HttpContent content)
    {
        if (content.Headers.ContentType == null)
            return false;

        var mediaType = content.Headers.ContentType.MediaType!;
        return mediaType.StartsWith("text/") ||
               mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildErrorLogString(HttpRequestMessage request, Exception ex, TimeSpan elapsed)
    {
        var sb = new StringBuilder(4096);

        sb.AppendLine();
        sb.AppendLine("┌─────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"│ API Request FAILED: [{request.Method}] {request.RequestUri}");
        sb.AppendLine("├─────────────────────────────────────────────────────────────────────────────");

        // Request headers
        sb.AppendLine("├─ Request Headers");
        foreach (var h in request.Headers)
            sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");

        if (request.Content != null)
        {
            foreach (var h in request.Content.Headers)
                sb.AppendLine($"│  {h.Key}: {string.Join(", ", h.Value)}");
        }

        // Exception info
        sb.AppendLine("├─ Exception");
        sb.AppendLine($"│  Type: {ex.GetType().FullName}");
        sb.AppendLine($"│  Message: {ex.Message}");

        if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
            sb.AppendLine($"│  StatusCode: {(int)httpEx.StatusCode.Value} {httpEx.StatusCode.Value}");

        if (ex.InnerException != null)
            sb.AppendLine($"│  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");

        // Stack trace (trimmed for readability)
        var stackTrace = ex.StackTrace ?? string.Empty;
        if (stackTrace.Length > 0)
        {
            sb.AppendLine("│  Stack:");
            foreach (var line in stackTrace.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    sb.AppendLine($"│    {trimmed}");
            }
        }

        sb.AppendLine($"└─────────────────────────────────Elapsed: {elapsed.TotalMilliseconds:0000} ms─────────────────────────────");

        return sb.ToString();
    }
}

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace AuraEcho.Logging;

public sealed class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    /// <summary>
    /// 敏感请求/响应头名单。命中后其值在日志中以 <c>***</c> 脱敏，避免 Token、Cookie 等凭据落盘。
    /// </summary>
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Auth-Token",
        "Api-Key",
    };

    private const string RedactedValue = "***";

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // 概要：始终以结构化字段记录，便于按字段检索与聚合。
            _logger.LogInformation(
                "HTTP {Method} {Uri} -> {StatusCode} in {ElapsedMs} ms",
                request.Method.Method,
                request.RequestUri,
                (int)response.StatusCode,
                (long)sw.Elapsed.TotalMilliseconds);

            // 明细（含头/体，已脱敏）：仅在 Debug 级别开启时构造，避免生产环境性能与安全负担。
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var logText = await BuildLogString(request, response, sw.Elapsed, cancellationToken);
                _logger.LogDebug("{HttpTrace}", logText);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "HTTP {Method} {Uri} 请求失败，耗时 {ElapsedMs} ms",
                request.Method.Method,
                request.RequestUri,
                (long)sw.Elapsed.TotalMilliseconds);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var logText = BuildErrorLogString(request, ex, sw.Elapsed);
                _logger.LogDebug("{HttpTrace}", logText);
            }

            throw;
        }
    }

    /// <summary>
    /// 输出一个头字段，命中敏感名单时对其值脱敏。
    /// </summary>
    private static void AppendHeader(StringBuilder sb, string name, IEnumerable<string> values)
    {
        var rendered = SensitiveHeaders.Contains(name)
            ? RedactedValue
            : string.Join(", ", values);
        sb.AppendLine($"│  {name}: {rendered}");
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
            AppendHeader(sb, h.Key, h.Value);

        if (request.Content != null)
        {
            foreach (var h in request.Content.Headers)
                AppendHeader(sb, h.Key, h.Value);

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
            AppendHeader(sb, h.Key, h.Value);

        if (response.Content != null)
        {
            foreach (var h in response.Content.Headers)
                AppendHeader(sb, h.Key, h.Value);
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
            AppendHeader(sb, h.Key, h.Value);

        if (request.Content != null)
        {
            foreach (var h in request.Content.Headers)
                AppendHeader(sb, h.Key, h.Value);
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

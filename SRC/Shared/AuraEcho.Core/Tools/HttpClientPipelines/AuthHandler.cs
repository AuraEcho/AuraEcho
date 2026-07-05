using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using Prism.Events;

namespace AuraEcho.Core.Tools.HttpClientPipelines;

/// <summary>
/// HTTP 管道处理器：自动附加 Bearer Token，并在收到 401 时尝试刷新 Token 后重试。
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider;
    private readonly IEventAggregator _eventAggregator;

    public AuthHandler(ITokenProvider tokenProvider, IEventAggregator eventAggregator)
    {
        _tokenProvider = tokenProvider;
        _eventAggregator = eventAggregator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenProvider.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // 检查是否被踢下线
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (responseBody.Contains("KICKED_OUT"))
        {
            _eventAggregator.GetEvent<KickedOutEvent>().Publish();
            return response;
        }

        if (_tokenProvider.Token is null)
            return response;

        // 刷新 Token
        if (!await _tokenProvider.TryRefreshTokenAsync())
        {
            _eventAggregator.GetEvent<SignInExpiredEvent>().Publish();
            return response;
        }

        // 使用新 Token 重试请求
        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        retryRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _tokenProvider.Token);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    /// <summary>
    /// 克隆 HttpRequestMessage，请求体 Stream 不能重复读取。
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
        };

        if (request.Content is not null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms, ct);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var (key, value) in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(key, value);
        }

        foreach (var (key, value) in request.Headers)
            clone.Headers.TryAddWithoutValidation(key, value);

        foreach (var opt in request.Options)
            clone.Options.TryAdd(opt.Key, opt.Value);

        return clone;
    }
}

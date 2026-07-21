using System.Net.Http;
using AuraEcho.Cloud.Helpers;
using AuraEcho.Cloud.V1.EndPoints;
using AuraEcho.Cloud.V1.Hub;
using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Tools;
using AuraEcho.Logging;
using Microsoft.Extensions.Logging;

namespace AuraEcho.Core.Services;

/// <summary>
/// <see cref="ITokenProvider"/> 的默认实现。
/// 集中管理 Token 状态、持久化与刷新，不依赖 UI 层事件或用户资料。
/// </summary>
public class TokenProvider : ITokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly AuthEndpoint _authEndpoint;
    private readonly IClock _clock;
    private readonly ILogger<LoggingHandler> _logger;

    public TokenProvider(IClock clock, ILogger<LoggingHandler> logger)
    {
        _clock = clock;
        _logger = logger;

        // 内部 HttpClient（不经过 AuthHandler），避免刷新 Token 时递归
        _httpClient = new HttpClient(new LoggingHandler(_logger)
        {
            InnerHandler = new HttpClientHandler()
        });
        var httpHelper = new HttpHelper(_httpClient);
        _authEndpoint = new AuthEndpoint(httpHelper);

        RefreshToken = SecureStore.Load(SecureStoreKeys.RefreshToken);
    }

    private string? _accessToken;
    public string? RefreshToken { get; private set; }
    private DateTimeOffset _expiresAt;

    /// <inheritdoc />
    public bool IsSignedIn => _accessToken is not null;

    /// <inheritdoc />
    string? IHubTokenProvider.Token => _accessToken;

    /// <inheritdoc />
    public void SetToken(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        _accessToken = accessToken;
        RefreshToken = refreshToken;
        _expiresAt = expiresAt;
        SecureStore.Save(SecureStoreKeys.RefreshToken, refreshToken);
    }

    /// <inheritdoc />
    public void ClearToken()
    {
        _accessToken = null;
        RefreshToken = null;
        SecureStore.Delete(SecureStoreKeys.RefreshToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryRefreshTokenAsync()
    {
        if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
                return false;

            if (!IsExpired())
                return true;

            ResponseResult<AuthResponse> result = 
                await _authEndpoint.RefreshTokenAsync(new RefreshTokenRequest 
                { 
                    RefreshToken = RefreshToken 
                });

            if (result is { Status: ResultStatus.Success, Data: not null })
            {
                SetToken(
                    result.Data.AccessToken,
                    result.Data.RefreshToken,
                    result.Data.ExpiresAt);
                return true;
            }

            ClearToken();
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsExpired()
        => _clock.UtcNow.AddSeconds(5) >= _expiresAt;
}

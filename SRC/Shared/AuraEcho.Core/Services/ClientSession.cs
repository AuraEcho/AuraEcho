using AuraEcho.Api.Models.V1.Auth;
using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Core.Tools.HttpClientPipelines;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Net.Http;
using System.Net.Http.Json;

namespace AuraEcho.Core.Services;

public class ClientSession : BindableBase, IClientSession
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly IClock _clock;
    private readonly IEventAggregator _eventAggregator;

    public ClientSession(IClock clock, IAppLogger logger, IEventAggregator eventAggregator)
    {
        _clock = clock;
        var logHandler = new LoggingHandler(logger);
        logHandler.InnerHandler = new HttpClientHandler();
        _httpClient = new HttpClient(logHandler);

        _eventAggregator = eventAggregator;
    }

    public bool IsSignedIn => AppToken is not null;

    public AppToken? AppToken { get; private set; }

    public UserProfile? CurrentUser
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private bool IsExpired()
    {
        return _clock.UtcNow >= AppToken.ExpiresAt;
    }

    public void SignIn(AuthResponse authResponse)
    {
        AppToken = new AppToken
        {
            AccessToken = authResponse.AccessToken,
            RefreshToken = authResponse.RefreshToken,
            ExpiresAt = authResponse.ExpiresAt
        };
        CurrentUser = authResponse.User.ToUserProfile();
        SecureStore.Save(SecureStoreKeys.RefreshToken, AppToken.RefreshToken);

        _eventAggregator.GetEvent<SignedInEvent>().Publish();
    }

    public void UpdateUserProfile(UserProfile userProfile)
    {
        CurrentUser = userProfile;
    }
    public void SignOut()
    {
        CurrentUser = null;
        AppToken = null;
        SecureStore.Delete(SecureStoreKeys.RefreshToken);

        _eventAggregator.GetEvent<SignedOutEvent>().Publish();
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            if (!IsExpired())
                return true;

            if (string.IsNullOrWhiteSpace(AppToken?.RefreshToken))
                return false;

            var response = await _httpClient.PostAsJsonAsync(
                Urls.RefreshToken(),
                new RefreshTokenRequest
                {
                    RefreshToken = AppToken.RefreshToken
                });
            if (!response.IsSuccessStatusCode)
                return false;

            var token = await response.Content.ReadFromJsonAsync<ResponseResult<AuthResponse>>();
            if (token is null)
                return false;

            SignIn(token.Data);

            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}

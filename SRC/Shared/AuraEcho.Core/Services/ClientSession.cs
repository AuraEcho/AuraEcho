using System.Diagnostics;
using AuraEcho.Cloud.V1.Hub;
using AuraEcho.Cloud.V1.Hub.Messages;
using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Cloud.V1.Models.Order;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.Telemetry;
using Prism.Events;
using Prism.Mvvm;

namespace AuraEcho.Core.Services;

/// <summary>
/// <see cref="IClientSession"/> 的默认实现。
/// </summary>
public class ClientSession : BindableBase, IClientSession
{
    private readonly ITokenProvider _tokenProvider;
    private readonly IHubClient _cloudHubClient;
    private readonly IEventAggregator _eventAggregator;
    private readonly ITelemetryService _telemetry;

    public ClientSession(
        IEventAggregator eventAggregator,
        IHubClient cloudHubClient,
        ITokenProvider tokenProvider,
        ITelemetryService telemetry)
    {
        _eventAggregator = eventAggregator;
        _cloudHubClient = cloudHubClient;
        _tokenProvider = tokenProvider;
        _telemetry = telemetry;

        _eventAggregator.GetEvent<SignInExpiredEvent>().Subscribe(SignOut);
    }

    /// <inheritdoc />
    public bool IsSignedIn => _tokenProvider.IsSignedIn;

    /// <inheritdoc />
    public UserProfile? CurrentUser
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public void SignIn(AuthResponse authResponse)
    {
        _tokenProvider.SetToken(
            authResponse.AccessToken,
            authResponse.RefreshToken,
            authResponse.ExpiresAt,
            authResponse.Jti);

        UpdateUserProfile(authResponse.User.ToUserProfile());

        _eventAggregator.GetEvent<SignedInEvent>().Publish();
        _telemetry.TrackEvent("Auth.SignIn");

        _ = ConnectCloudHubAsync();
    }

    /// <summary>
    /// 连接 CloudHub
    /// </summary>
    /// <returns></returns>
    private async Task ConnectCloudHubAsync()
    {
        try
        {
            SubScribeCloudHubEvents();
            await _cloudHubClient.ConnectAsync();
            Debug.WriteLine($"CloudHub 已连接");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CloudHub 连接失败 {ex.Message}");
        }

        // 订阅 CloudHub 事件
        void SubScribeCloudHubEvents()
        {
            _cloudHubClient.Subscribe<OrderPaidMessage, OrderPaymentDetails>(
                payload =>
                {
                    Debug.WriteLine($"收到订单支付成功消息，订单号：{payload.OrderId}");
                    _eventAggregator.GetEvent<OrderPaidEvent>().Publish(payload);
                });

            // 单设备登录
            _cloudHubClient.Subscribe<SignedInElsewhereMessage, string>(
                newJti =>
                {
                    if (newJti == _tokenProvider.Jti)
                    {
                        Debug.WriteLine("收到 AccountSignedInElsewhere，但 jti 与本机一致，忽略");
                        return;
                    }

                    Debug.WriteLine("收到 AccountSignedInElsewhere，账号已在其他设备登录");
                    _eventAggregator.GetEvent<KickedOutEvent>().Publish();
                });

            // 公告变更
            _cloudHubClient.Subscribe<AnnouncementChangedMessage, string>(
                _ =>
                {
                    Debug.WriteLine("收到 AnnouncementChanged，公告已变更");
                    _eventAggregator.GetEvent<AnnouncementsChangedEvent>().Publish();
                });
        }
    }


    /// <summary>
    /// 断开 CloudHub 连接
    /// </summary>
    /// <returns></returns>
    private async Task DisconnectCloudHubAsync()
    {
        try
        {
            _cloudHubClient.ClearSubscriptions();
            await _cloudHubClient.DisconnectAsync();
            Debug.WriteLine($"CloudHub 已断开连接");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CloudHub 断开连接失败 {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void UpdateUserProfile(UserProfile userProfile)
    {
        CurrentUser = userProfile;
    }

    /// <inheritdoc />
    public void SignOut()
    {
        _ = DisconnectCloudHubAsync();
        CurrentUser = null;
        _tokenProvider.ClearToken();

        _eventAggregator.GetEvent<SignedOutEvent>().Publish();
        _telemetry.TrackEvent("Auth.SignOut");
    }
}

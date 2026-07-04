using AuraEcho.ClientApi.V1.Order;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Events;
using AuraEcho.PluginContracts.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Prism.Events;

namespace AuraEcho.Core.Services;

public class CloudPushService
{
    private readonly IAppLogger _logger;
    private readonly IEventAggregator _eventAggregator;
    private const string ORDER_PAID_EVENT_NAME = "OrderPaid";
    public CloudPushService(IAppLogger logger, IEventAggregator eventAggregator)
    {
        _logger = logger;
        _eventAggregator = eventAggregator;
    }

    private HubConnection _connection;
    private readonly string _hubUrl = $"{Urls.ServerUrl}/hubs/main";

    public async Task ConnectAsync(Func<Task<string?>> tokenProvider)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options => options.AccessTokenProvider = tokenProvider)
            .WithAutomaticReconnect() // 自动重连
            .Build();

        _connection.On<OrderPaymentDetails>(
            ORDER_PAID_EVENT_NAME, 
            (data) =>
            {
                _logger.Information($"CloudPush: OrderPaid");
                data.PayTime = DateTime.SpecifyKind(data.PayTime, DateTimeKind.Utc).ToLocalTime();
                _eventAggregator.GetEvent<OrderPaidEvent>().Publish(data); 
            });

        try
        {
            await _connection.StartAsync();
            _logger.Debug("SignalR 已连接");
        }
        catch (Exception ex)
        {
            _logger.Error($"SignalR 连接失败: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}

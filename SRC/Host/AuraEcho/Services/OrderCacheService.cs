using AuraEcho.Cloud.V1.Models.Order;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AuraEcho.Services;

public record struct OrderPayUrlCacheKey(Guid SkuId, PaymentChannel PaymentChannel);

/// <summary>
/// 订单缓存服务
/// </summary>
public class OrderCacheService
{
    private readonly MemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public OrderCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });
    }

    public void Create(OrderPayUrlCacheKey key, CreateOrderResponse order)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1
        };

        _cache.Set(key, order, options);
    }

    public void Remove(OrderPayUrlCacheKey key)
    {
        _cache.Remove(key);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public async Task<CreateOrderResponse?> GetOrFetchAsync(
        OrderPayUrlCacheKey key,
        Func<OrderPayUrlCacheKey, Task<CreateOrderResponse?>> fetcher)
    {
        if (TryGet(key, out CreateOrderResponse? order))
            return order;

        CreateOrderResponse? fetchResult = await fetcher(key);

        if (fetchResult is null)
            return null;

        Create(key, fetchResult);
        return fetchResult;
    }

    public bool TryGet(OrderPayUrlCacheKey key, out CreateOrderResponse? order)
    {
        if (_cache.TryGetValue(key, out object? rawValue) && rawValue is CreateOrderResponse v)
        {
            order = v;
            return true;
        }

        order = default;
        return false;
    }
}

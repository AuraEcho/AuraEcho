using AuraEcho.Cloud.V1.Models.Order;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AuraEcho.Services;

public record struct OrderPayUrlCacheKey(Guid SkuId, PaymentChannel PaymentChannel);

public class OrderPayUrlCacheService
{
    private readonly MemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public OrderPayUrlCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });
    }

    public void Create(OrderPayUrlCacheKey key, string payUrl)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1
        };

        _cache.Set(key, payUrl, options);
    }

    public void Remove(OrderPayUrlCacheKey key)
    {
        _cache.Remove(key);
    }

    public async Task<string?> GetOrFetchAsync(OrderPayUrlCacheKey key, Func<OrderPayUrlCacheKey, Task<string?>> fetcher)
    {
        if (TryGet(key, out string? payUrl))
            return payUrl;

        string? fetchResult = await fetcher(key);

        if (fetchResult == null)
            return null;

        Create(key, fetchResult);
        return fetchResult;
    }

    public bool TryGet(OrderPayUrlCacheKey key, out string? payUrl)
    {
        if (_cache.TryGetValue(key, out object? rawValue) && rawValue is string v)
        {
            payUrl = v;
            return true;
        }

        payUrl = default;
        return false;
    }
}


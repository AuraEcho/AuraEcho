using AuraEcho.ClientApi.V1.Common;
using AuraEcho.ClientApi.V1.Order;
using AuraEcho.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AuraEcho.Services;

public class SkuOrderCacheService : ISkuOrderCacheService
{
    private readonly MemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public SkuOrderCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });
    }

    public async Task<ResponseResult<CreateOrderResponse>?> GetOrFetchSkuOrderAsync(
        Guid ResourceId,
        Guid skuId, 
        PaymentChannel paymentChannel,
        Func<Guid, PaymentChannel, Task<ResponseResult<CreateOrderResponse>>?> orderFetcher)
    {
        string cacheKey = $"sku_payurl:{ResourceId:N}:{skuId:N}:{paymentChannel}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            // 针对 SizeLimit，指定条目的 Size
            entry.Size = 1;

            return await orderFetcher(skuId, paymentChannel);
        }) ?? null;
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <param name="ResourceId"></param>
    /// <param name="skuId"></param>
    /// <param name="paymentChannel"></param>
    public void InvalidateCache(Guid ResourceId, Guid skuId, PaymentChannel paymentChannel)
    {
        _cache.Remove($"sku_payurl:{ResourceId:N}:{skuId:N}:{paymentChannel}");
    }
}

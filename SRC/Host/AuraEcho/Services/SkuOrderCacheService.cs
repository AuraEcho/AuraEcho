using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
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
        Guid skuId, 
        PaymentChannel paymentChannel,
        Func<Guid, PaymentChannel, Task<ResponseResult<CreateOrderResponse>>?> orderFetcher)
    {
        string cacheKey = $"sku_payurl:{skuId:N}:{paymentChannel}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            // 针对 SizeLimit，指定条目的 Size
            entry.Size = 1;

            return await orderFetcher(skuId, paymentChannel);
        }) ?? null;
    }

    /// <summary>
    /// 移除 sku 对应的缓存
    /// </summary>
    /// <param name="skuId"></param>
    public void InvalidateCache(Guid skuId, PaymentChannel paymentChannel)
    {
        _cache.Remove($"sku_payurl:{skuId:N}:{paymentChannel}");
    }
}

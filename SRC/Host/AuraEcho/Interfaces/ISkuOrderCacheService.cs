using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Cloud.V1.Models.Order;
using System;
using System.Threading.Tasks;

namespace AuraEcho.Interfaces;

public interface ISkuOrderCacheService
{
    Task<ResponseResult<CreateOrderResponse>?> GetOrFetchSkuOrderAsync(
        Guid ResourceId,
        Guid skuId, 
        PaymentChannel paymentChannel,
        Func<Guid, PaymentChannel,Task<ResponseResult<CreateOrderResponse>>?> priceUrlFetcher);

    void InvalidateCache(Guid ResourceId, Guid skuId, PaymentChannel paymentChannel);
}

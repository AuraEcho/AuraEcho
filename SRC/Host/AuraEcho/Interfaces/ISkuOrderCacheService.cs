using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
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

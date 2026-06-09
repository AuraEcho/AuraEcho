using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Order;
using System;
using System.Threading.Tasks;

namespace AuraEcho.Interfaces;

public interface ISkuOrderCacheService
{
    Task<ResponseResult<CreateOrderResponse>?> GetOrFetchSkuOrderAsync(
        Guid skuId, 
        PaymentChannel paymentChannel,
        Func<Guid, PaymentChannel,Task<ResponseResult<CreateOrderResponse>>?> priceUrlFetcher);

    void InvalidateCache(Guid skuId, PaymentChannel paymentChannel);
}

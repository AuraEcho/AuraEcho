using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class SkuRepository : ISkuRepository
{
    private HttpHelper _httpHelper;
    public SkuRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }

    public async Task<List<Sku>> GetResourceSkusAsync(Guid resourceId)
    {
        var result = await _httpHelper.GetAsync<GetResourceSkusResponse>(Urls.GetResourceSkus(resourceId));
        if (result is null) return [];

        List<Sku> skus =
            [.. result.Skus.Select(p => new Sku
            {
                Id = p.Id.Value,
                SalePrice = p.SalePrice,
                OriginalPrice = p.OriginalPrice,
                ResourceId = p.ResourceId,
                ResourceType = p.ResourceType,
                Type = p.Type,
                IsActive = p.IsActive
            })];

        return skus;
    }
}

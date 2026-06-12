using AuraEcho.Api.Models.V1.Sku;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Models;

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
            result.Skus
                  .Select(p => new Sku
                  {
                      Id = p.Id.Value,
                      SalePrice = p.SalePrice,
                      OriginalPrice = p.OriginalPrice,
                      ResourceId = p.ResourceId,
                      ResourceType = p.ResourceType,
                      Type = (LicenseType)(int)p.Type,
                      IsActive = p.IsActive,
                      Ordinal = p.Ordianl
                  }).OrderBy(s => s.Ordinal)
                  .ToList();

        return skus;
    }
}

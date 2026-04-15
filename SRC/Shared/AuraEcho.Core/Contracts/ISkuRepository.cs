using AuraEcho.Core.Models;

namespace AuraEcho.Core.Contracts;

public interface ISkuRepository
{
    Task<List<Sku>> GetResourceSkusAsync(Guid resourceId);
}

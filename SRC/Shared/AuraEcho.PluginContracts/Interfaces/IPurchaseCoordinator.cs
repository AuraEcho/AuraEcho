using System;
using System.Threading.Tasks;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IPurchaseCoordinator
    {
        Task<bool> RequestPurchaseAsync(Guid resourceId);
    }
}

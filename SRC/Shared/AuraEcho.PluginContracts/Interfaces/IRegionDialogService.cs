using System.Threading.Tasks;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IRegionDialogService
    {
        Task<RegionDialogResult> ShowDialogAsync(string regionName, string target, RegionDialogParameter parameter);
    }
}

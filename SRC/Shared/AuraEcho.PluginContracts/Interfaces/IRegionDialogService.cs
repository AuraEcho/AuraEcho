using System.Threading.Tasks;
using AuraEcho.PluginContracts.Models;
using Prism.Regions;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IRegionDialogService
    {
        Task<RegionDialogResult> ShowDialogAsync(string regionName, string target, NavigationParameters parameters);
    }
}

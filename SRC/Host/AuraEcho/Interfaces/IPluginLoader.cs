using System.Threading.Tasks;
using AuraEcho.Core.Models;

namespace AuraEcho.Interfaces;

public interface IPluginLoader
{
    Task<AppPlugin> LoadPluginAsync(UserPluginModel userPluginModel);
}

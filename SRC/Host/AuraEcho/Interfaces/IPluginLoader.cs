using System.Threading.Tasks;
using AuraEcho.Core.Models;
using AuraEcho.Domain;

namespace AuraEcho.Interfaces;

public interface IPluginLoader
{
    Task<AppPlugin> LoadPluginAsync(UserPluginModel userPluginModel);
}

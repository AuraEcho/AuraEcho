using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.Core.Services;

public class PathProvider : IPathProvider
{
    public string PluginsRootPath => ApplicationPaths.Plugins;

    public string DataRootPath => ApplicationPaths.Data;
}

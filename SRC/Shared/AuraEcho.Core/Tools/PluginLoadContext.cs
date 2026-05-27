using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace AuraEcho.Core.Tools;

/// <summary>
/// 自定义程序集加载上下文，用于加载插件程序集
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath == null)
            return null;

        return LoadAssemblyFromStream(assemblyPath);
    }

    public Assembly LoadEntryAssembly(string assemblyPath)
    {
        return LoadAssemblyFromStream(assemblyPath);
    }

    private Assembly LoadAssemblyFromStream(string assemblyPath)
    {
        using var ms = new MemoryStream(File.ReadAllBytes(assemblyPath));
        return LoadFromStream(ms);
    }
}

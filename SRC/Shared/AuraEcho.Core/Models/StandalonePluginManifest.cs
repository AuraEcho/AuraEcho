namespace AuraEcho.Core.Models;

public class StandalonePluginManifest : PluginManifest
{
    public string CommandLineArgs
    {
        get;
        set => SetProperty(ref field, value);
    }
}

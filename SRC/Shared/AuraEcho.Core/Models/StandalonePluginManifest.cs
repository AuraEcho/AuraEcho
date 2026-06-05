namespace AuraEcho.Core.Models;

public class StandalonePluginManifest : PluginManifest
{
    public string EntryFileName
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string CommandLineArgs
    {
        get;
        set => SetProperty(ref field, value);
    }
}

namespace AuraEcho.Core.Models;

public class LocalWebPluginManifest : PluginManifest
{
    public string EntryFileName
    {
        get;
        set => SetProperty(ref field, value);
    }
}

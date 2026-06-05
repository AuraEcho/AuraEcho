namespace AuraEcho.Core.Models;

public class RemoteWebPluginManifest : PluginManifest
{
    public string RemoteUrl
    {
        get;
        set => SetProperty(ref field, value);
    }
}

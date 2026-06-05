namespace AuraEcho.Core.Models
{
    public class NativePluginManifest : PluginManifest
    {
        public string EntryFileName
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}

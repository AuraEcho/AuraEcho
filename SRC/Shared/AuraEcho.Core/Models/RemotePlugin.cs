using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public class RemotePlugin : BindableBase
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public Guid IconFileId { get; set; }
    public Guid BannerFileId { get; set; }
    public bool IsAcquired { get; set; }
    public DateTime CreateTime { get; set; }
    public int UserCount { get; set; }

    public List<PluginPackage> Versions
    {
        get;
        set => SetProperty(ref field, value);
    }

    public List<PluginScreenshot> Screenshots
    {
        get;
        set => SetProperty(ref field, value);
    }
}

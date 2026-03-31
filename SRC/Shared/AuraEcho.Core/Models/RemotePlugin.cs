using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public class RemotePlugin : BindableBase
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public Guid IconFileId { get; set; }
    public bool IsAcquired { get; set; }
    public DateTime CreateTime { get; set; }

    private List<PluginPackage> _versions;
    public List<PluginPackage> Versions
    {
        get => _versions;
        set => SetProperty(ref _versions, value);
    }
}

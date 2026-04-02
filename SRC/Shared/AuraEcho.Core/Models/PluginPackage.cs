using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public class PluginPackage : BindableBase
{
    public Guid Id { get; set; }
    public Guid PluginId { get; set; }

    public string Version { get; set; }
    public string ReleaseNotes { get; set; }
    public Guid? FileId { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public DateTime? CreateTime { get; set; }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }
}

namespace AuraEcho.Core.Models.Api;

public class GetPluginLatestVersionResponse
{
    public Guid Id { get; set; }
    public Guid PluginId { get; set; }

    public string Version { get; set; }
    public string ReleaseNotes { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public DateTime? CreateTime { get; set; }
}

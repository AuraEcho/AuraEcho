namespace AuraEcho.Core.Models;

public class AppVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public Guid? FullFileId { get; set; }
    public string FullFileUrl { get; set; }
    public string? FullFileName { get; set; }
    public long? FullFileSize { get; set; }

    public Guid? UpdateFileId { get; set; }
    public string UpdateFileUrl { get; set; }
    public string? UpdateFileName { get; set; }
    public long? UpdateFileSize { get; set; }
    public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;
}

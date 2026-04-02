namespace AuraEcho.Core.Models.Api;

public class ListPluginItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public Guid IconFileId { get; set; }
    public Guid BannerFileId { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsAcquired { get; set; }
    public int UserCount { get; set; }
}

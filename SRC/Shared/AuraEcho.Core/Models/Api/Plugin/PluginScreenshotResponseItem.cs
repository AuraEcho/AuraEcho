namespace AuraEcho.Core.Models.Api;

public class PluginScreenshotResponseItem
{
    public Guid Id { get; set; }
    public Guid PluginId { get; set; }
    public Guid FileId { get; set; }
    public int Order { get; set; }
}

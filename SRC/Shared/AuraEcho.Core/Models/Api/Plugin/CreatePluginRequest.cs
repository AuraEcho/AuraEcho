namespace AuraEcho.Core.Models.Api;

public class CreatePluginRequest
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public Guid IconFileId { get; set; }
}

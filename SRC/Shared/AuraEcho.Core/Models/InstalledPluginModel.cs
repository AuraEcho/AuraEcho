using AuraEcho.ClientApi.V1.Plugin;

namespace AuraEcho.Core.Models;

public class InstalledPluginModel 
{
    public Guid Id { get; set; }
    public Guid PluginId { get; set; }
    public PluginType PluginType { get; set; }
    public string? InstallPath { get; set; }
    public DateTime InstaledAt { get; set; }
    public string Version { get; set; }
    public bool IsSetup { get; set; }
}
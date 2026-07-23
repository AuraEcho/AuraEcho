using AuraEcho.Domain;

namespace AuraEcho.Persistence.Entities;

public class UserPlugin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public Guid LocalPluginId { get; set; }
    public InstalledPlugin? LocalPlugin { get; set; }

    public PluginPlanStatus Status { get; set; }
}

using AuraEcho.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace AuraEcho.Core.Data.Entities;

public class UserPlugin
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public Guid LocalPluginId { get; set; }
    public LocalPlugin? LocalPlugin { get; set; }

    public PluginPlanStatus Status { get; set; }
}

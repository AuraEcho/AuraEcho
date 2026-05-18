using System;

namespace AuraEcho.Models;

public class PluginUninstallPendingRestart : PendingRestartItem
{
    public Guid PluginId { get; set; }
    public string IconPath { get; set; }
}

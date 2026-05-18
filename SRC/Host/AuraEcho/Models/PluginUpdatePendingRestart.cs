using System;

namespace AuraEcho.Models;

public class PluginUpdatePendingRestart : PendingRestartItem
{
    public Guid PluginId { get; set; }
    public string IconPath { get; set; }
    public Version CurrentVersion { get; set; }
    public Version LatestVersion { get; set; }
}

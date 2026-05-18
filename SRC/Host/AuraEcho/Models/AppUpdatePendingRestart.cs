using System;

namespace AuraEcho.Models;

public class AppUpdatePendingRestart : PendingRestartItem
{
    public Version LatestVersion { get; set; }
}

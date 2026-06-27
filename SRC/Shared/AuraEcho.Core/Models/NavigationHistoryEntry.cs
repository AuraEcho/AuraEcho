using Prism.Regions;

namespace AuraEcho.Core.Models;

public record NavigationHistoryEntry(string RegionName, string ViewName, NavigationParameters? Parameters);

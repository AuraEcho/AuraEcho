using System.Collections.Generic;
using AuraEcho.Core.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Interfaces;

public interface IThemeManager
{
    /// <summary>
    /// 当前主题
    /// </summary>
    AppTheme CurrentTheme { get; set; }

    void ApplyTheme(AppTheme appTheme);

    void AttachPluginTheme(AppPlugin plugin);

    void AttachPluginThemes(IEnumerable<AppPlugin> plugins);
}

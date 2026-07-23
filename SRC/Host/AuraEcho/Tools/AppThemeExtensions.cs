using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Tools;

public static class AppThemeExtensions
{
    /// <summary>
    /// 获取主题的亮度分类（基础主题）。
    /// </summary>
    public static AppTheme GetBaseTheme(this AppTheme theme)
        => theme switch
        {
            AppTheme.Dark => AppTheme.Dark,
            AppTheme.Light => AppTheme.Light,
            _ => AppTheme.Dark,
        };
}

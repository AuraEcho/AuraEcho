namespace AuraEcho.PluginContracts.Models
{
    public static class AppThemeExtensions
    {
        /// <summary>
        /// 获取主题的亮度分类（基础主题）。
        /// </summary>
        public static AppTheme GetBaseTheme(this AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark:
                    return AppTheme.Dark;
                case AppTheme.Light:
                    return AppTheme.Light;
                default:
                    return AppTheme.Dark;
            }
        }
    }
}

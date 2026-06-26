using System;
using System.Globalization;
using System.Windows.Data;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Converters;

public class LanguageDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppLanguage al) return null;

        return al switch
        {
            AppLanguage.ChineseSimplified => "简体中文",
            AppLanguage.English => "English",
            AppLanguage.Korean => "한국어",
            AppLanguage.Japanese => "日本語",
            _ => throw new Exception("不支持的语言选项")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

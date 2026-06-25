using AuraEcho.Core.Strings;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AuraEcho.Converters;

/// <summary>
/// Converts an enum value to a localized display string by looking up
/// "{EnumTypeName}_{EnumMemberName}" from the Labels resource manager.
/// Falls back to the enum member name if no localized string is found.
/// </summary>
public class EnumLocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            string key = $"{value.GetType().Name}_{enumValue}";
            string? localized = Labels.ResourceManager.GetString(key);
            return localized ?? enumValue.ToString();
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

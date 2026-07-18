using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace AuraEcho.Converters;

/// <summary>
/// 此转换器非通用转换器（用于设置-常规设置页面的自适应布局）
/// </summary>
public class WidthToDockConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width) return Dock.Right;

        if (!double.TryParse(parameter as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
            return Dock.Right;

        return width < threshold ? Dock.Bottom : Dock.Right;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

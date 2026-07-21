using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    /// <summary>
    /// <see cref="Color"/> 转换为 <see cref="SolidColorBrush"/> 的转换器"/>
    /// </summary>
    [ValueConversion(typeof(Color), typeof(SolidColorBrush))]
    public class ColorToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

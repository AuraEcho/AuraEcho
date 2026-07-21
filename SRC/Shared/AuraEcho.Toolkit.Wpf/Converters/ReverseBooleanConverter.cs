using System;
using System.Globalization;
using System.Windows.Data;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    /// <summary>
    /// 反转 Boolean 值
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class ReverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !(bool?)value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => !(bool?)value;
    }
}
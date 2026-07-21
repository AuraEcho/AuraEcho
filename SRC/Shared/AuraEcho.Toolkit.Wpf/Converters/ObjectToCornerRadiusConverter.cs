using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    public class ObjectToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int v = System.Convert.ToInt32(value);
            return new CornerRadius(v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

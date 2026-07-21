using System;
using System.Globalization;
using System.Windows.Data;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(bool)value)
                return Binding.DoNothing;

            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}

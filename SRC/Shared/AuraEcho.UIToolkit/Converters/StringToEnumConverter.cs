using System;
using System.Globalization;
using System.Windows.Data;

namespace AuraEcho.UIToolkit.Converters
{
    [ValueConversion(typeof(string), typeof(Enum))]
    public class StringToEnumConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ($"{value}" == String.Empty)
            {
                return null;
            }

            try
            {
                return Enum.Parse(EnumType, $"{value}");
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString();
    }
}


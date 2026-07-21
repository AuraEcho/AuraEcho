using System;
using System.Globalization;
using System.Windows.Data;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    public class FileLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] units = { "B", "KB", "MB", "GB", "TB" };
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < units.Length - 1)
                {
                    order++;
                    len /= 1024;
                }

                string format = order <= 1 ? "{0:0} {1}" : "{0:0.#} {1}";
                return string.Format(format, len, units[order]);
            }

            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

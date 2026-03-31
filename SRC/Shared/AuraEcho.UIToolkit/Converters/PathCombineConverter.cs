using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace AuraEcho.UIToolkit.Converters
{
    public class PathCombineConverter : MarkupExtension, IMultiValueConverter
    {
        public PathCombineConverter _instance;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var paths = values
                .OfType<string>()
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (paths.Length == 0)
                return null;

            try
            {
                return Path.Combine(paths);
            }
            catch
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PathCombineConverter does not support ConvertBack.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
#if NET10_0_OR_GREATER
            => _instance ??= new PathCombineConverter();
#elif NET472
            => _instance ?? (_instance = new PathCombineConverter());
#endif
    }
}

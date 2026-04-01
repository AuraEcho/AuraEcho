using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AuraEcho.Core.Constants;

namespace AuraEcho.Converters;

public class FileIdToUrlConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Guid fileId) return null;
        return $"{Urls.ServerUrl}/api/v1/file/download?fileId={fileId}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

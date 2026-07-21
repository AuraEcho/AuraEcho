using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AuraEcho.Toolkit.Wpf.Tools;

namespace AuraEcho.Toolkit.Wpf.Converters;

[ValueConversion(typeof(string), typeof(BitmapSource))]
public class QRCodeToBitmapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string url
        ? QRCodeUtil.GenerateQRCode(url)
        : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

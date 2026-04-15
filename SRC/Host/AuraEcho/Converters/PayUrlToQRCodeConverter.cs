using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AuraEcho.Converters;

[ValueConversion(typeof(string), typeof(BitmapSource))]
public class PayUrlToQRCodeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
        => value is string url 
        ? Tools.QRCodeUtil.GenerateQRCode(url) 
        : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AuraEcho.Cloud.V1.Models.Order;

namespace AuraEcho.Converters;

[ValueConversion(typeof(PaymentChannel), typeof(Brush))]
public class PaymentChannelBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var channel = (PaymentChannel)value;
        return channel switch
        {
            PaymentChannel.Alipay => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4377FA")),
            PaymentChannel.Wxpay => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2dc100")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

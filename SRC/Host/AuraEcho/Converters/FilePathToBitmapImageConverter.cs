using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AuraEcho.Core.Constants;

namespace AuraEcho.Converters;

internal class FilePathToBitmapImageConverter : IValueConverter
{
    public int DecodePixelHeight { get; set; } = 100;
    public int DecodePixelWidth { get; set; } = 100;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string filePath) return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.DecodePixelHeight = DecodePixelHeight;
        bitmap.DecodePixelWidth = DecodePixelWidth;
        bitmap.EndInit();
        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AuraEcho.UIToolkit.Converters
{
    [ValueConversion(typeof(BitmapSource), typeof(SolidColorBrush))]
    public class OptimizedBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is BitmapSource bs)) return new SolidColorBrush(Colors.Transparent);

            var scaled = new TransformedBitmap(bs, new ScaleTransform(50.0 / bs.PixelWidth, 50.0 / bs.PixelHeight));
            int width = scaled.PixelWidth;
            int height = scaled.PixelHeight;
            byte[] pixels = new byte[width * height * 4];
            scaled.CopyPixels(pixels, width * 4, 0);

            var colorCandidates = new List<System.Drawing.Color>();

            for (int i = 0; i < pixels.Length; i += 4)
            {
                var c = System.Drawing.Color.FromArgb(pixels[i + 2], pixels[i + 1], pixels[i]);
                float hue = c.GetHue();
                float sat = c.GetSaturation();
                float bri = c.GetBrightness();

                // 避开极黑、极白和极灰的颜色
                if (bri > 0.15f && bri < 0.85f && sat > 0.2f)
                {
                    colorCandidates.Add(c);
                }
            }

            // 如果没找到合适颜色，退而求其次取平均值或预设色
            if (!colorCandidates.Any()) return Colors.DarkGray;

            // 统计出现频率最高的颜色块
            var bestColor = colorCandidates
                .GroupBy(c => new { R = c.R / 10, G = c.G / 10, B = c.B / 10 }) // 粗略分组提高聚合度
                .OrderByDescending(g => g.Count())
                .First().First();

            return new SolidColorBrush(Color.FromRgb(bestColor.R, bestColor.G, bestColor.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

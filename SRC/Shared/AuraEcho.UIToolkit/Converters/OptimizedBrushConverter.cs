using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AuraEcho.UIToolkit.Converters
{
    /// <summary>
    /// 从图片中提取平均背景色
    /// </summary>
    [ValueConversion(typeof(BitmapSource), typeof(Brush))]
    public class OptimizedBrushConverter : IValueConverter
    {
        /// <summary> 采样时将图片长边缩放到此尺寸以内。 </summary>
        private const int MaxSampleDimension = 150;

        /// <summary> 颜色量化级数（每个通道）。 </summary>
        private const int QuantizeLevels = 28;

        /// <summary> 输出颜色的饱和度保留比例（0=全灰，1=原始饱和度）</summary>
        private const float SaturationRetention = 0.90f;

        /// <summary> 左侧渐变点的明度缩放系数。 </summary>
        private const float LeftDarkenFactor = 0.78f;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is BitmapSource bs))
                return new SolidColorBrush(Color.FromRgb(32, 32, 32));

            // 缩放采样
            BitmapSource sampled = ScaleForSampling(bs);
            int width = sampled.PixelWidth;
            int height = sampled.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            sampled.CopyPixels(pixels, stride, 0);

            // 构建加权颜色直方图
            var histogram = BuildWeightedHistogram(pixels, width, height);

            if (histogram.Count == 0)
                return new SolidColorBrush(Color.FromRgb(32, 32, 32));

            // 取权重最高的单一簇作为主色
            var bestBucket = histogram
                .OrderByDescending(kvp => kvp.Value.TotalWeight)
                .First().Value;

            Color dominant = bestBucket.ToColor();

            // 柔化饱和度
            Color dampened = DampenSaturation(dominant, SaturationRetention);

            // 生成 2 段水平渐变
            return CreateTransitionGradient(dampened);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // 采样

        private static BitmapSource ScaleForSampling(BitmapSource source)
        {
            double scale = Math.Min(
                MaxSampleDimension / (double)source.PixelWidth,
                MaxSampleDimension / (double)source.PixelHeight);

            if (scale >= 1.0) return source;
            return new TransformedBitmap(source, new ScaleTransform(scale, scale));
        }

        /// <summary>
        /// 构建带位置权重的颜色直方图
        /// </summary>
        private static Dictionary<int, ColorBucket> BuildWeightedHistogram(
            byte[] pixels, int width, int height)
        {
            var histogram = new Dictionary<int, ColorBucket>();

            for (int y = 0; y < height; y++)
            {
                float yNorm = (float)y / height;
                // 垂直居中区域略加权（0.8 ~ 1.0）
                float verticalWeight = 1.0f - Math.Abs(yNorm - 0.5f) * 0.4f;

                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    float r = pixels[i + 2]; // BGRA → R
                    float g = pixels[i + 1]; // G
                    float b = pixels[i + 0]; // B

                    // HSL
                    float maxC = Math.Max(r, Math.Max(g, b));
                    float minC = Math.Min(r, Math.Min(g, b));
                    float l = (maxC + minC) / 2f / 255f;
                    float delta = maxC - minC;
                    float s = delta < 1f
                        ? 0f
                        : l > 0.5f
                            ? delta / (2f * 255f - maxC - minC)
                            : delta / (maxC + minC);

                    // 排除极端像素
                    if (l < 0.04f || l > 0.96f) continue;
                    if (s < 0.06f) continue;

                    // 位置权重：左侧×1.6，右侧 ×1.0
                    float xNorm = (float)x / width;
                    float posWeight = 0.6f + (1.0f - xNorm) * 0.6f;

                    // 饱和度偏好：0.2–0.85 区间等权（1.0），极低和极端饱和才降权
                    float satPreference;
                    if (s < 0.15f)
                        satPreference = s / 0.15f * 0.4f + 0.1f;  // 0.1 ~ 0.5，接近灰色降权
                    else if (s > 0.90f)
                        satPreference = 1.0f - (s - 0.90f) * 5f;  // >0.9 开始衰减，极度刺眼
                    else
                        satPreference = 1.0f;  // 正常区间全权重

                    float totalWeight = posWeight * verticalWeight * satPreference;

                    // 量化
                    int qR = (int)(r / 255f * QuantizeLevels);
                    int qG = (int)(g / 255f * QuantizeLevels);
                    int qB = (int)(b / 255f * QuantizeLevels);
                    int key = (qR << 16) | (qG << 8) | qB;

                    if (histogram.TryGetValue(key, out ColorBucket bucket))
                    {
                        bucket.Add(r, g, b, totalWeight);
                    }
                    else
                    {
                        histogram[key] = new ColorBucket(r, g, b, totalWeight);
                    }
                }
            }

            return histogram;
        }

        // ─── 颜色处理 ────────────────────────────────────────────────

        /// <summary>
        /// 将颜色向灰色方向混合以降低饱和度，适合大面积背景使用。
        /// factor: 0=全灰，1=保留原始饱和度。
        /// </summary>
        private static Color DampenSaturation(Color c, float factor)
        {
            // 感知亮度（人眼对各通道敏感度不同）
            float gray = c.R * 0.299f + c.G * 0.587f + c.B * 0.114f;
            return Color.FromRgb(
                ClampByte(gray + (c.R - gray) * factor),
                ClampByte(gray + (c.G - gray) * factor),
                ClampByte(gray + (c.B - gray) * factor));
        }

        /// <summary>
        /// 生成 2 段水平渐变
        /// </summary>
        private static Brush CreateTransitionGradient(Color dampened)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
            };

            // 左侧只降亮度保留饱和度，保持色彩连续性
            Color leftColor = Color.FromRgb(
                ClampByte(dampened.R * LeftDarkenFactor),
                ClampByte(dampened.G * LeftDarkenFactor),
                ClampByte(dampened.B * LeftDarkenFactor));

            brush.GradientStops.Add(new GradientStop(leftColor, 0.0));
            brush.GradientStops.Add(new GradientStop(dampened, 1.0));

            return brush;
        }

        private static byte ClampByte(float value)
            => (byte)(value < 0 ? 0 : value > 255 ? 255 : value);


        // 直方图桶：累积加权颜色值，最终输出加权平均色。
        private sealed class ColorBucket
        {
            public float WeightedR;
            public float WeightedG;
            public float WeightedB;
            public float TotalWeight;

            public ColorBucket(float r, float g, float b, float weight)
            {
                WeightedR = r * weight;
                WeightedG = g * weight;
                WeightedB = b * weight;
                TotalWeight = weight;
            }

            public void Add(float r, float g, float b, float weight)
            {
                WeightedR += r * weight;
                WeightedG += g * weight;
                WeightedB += b * weight;
                TotalWeight += weight;
            }

            public Color ToColor()
            {
                if (TotalWeight <= 0) return Color.FromRgb(64, 64, 64);
                return Color.FromRgb(
                    ClampByte(WeightedR / TotalWeight),
                    ClampByte(WeightedG / TotalWeight),
                    ClampByte(WeightedB / TotalWeight));
            }
        }
    }
}

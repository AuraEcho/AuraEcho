using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace AuraEcho.UIToolkit.Converters
{
    public class StringToImageSourceConverter : MarkupExtension, IValueConverter
    {
        private static StringToImageSourceConverter _instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string path) || String.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (!File.Exists(path)) return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);

                // 在加载时就完成解码, 在 UI 渲染时不需要再次触发解码, 避免滑动列表卡顿。
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null; // TODO: 默认图
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
#if NET10_0_OR_GREATER
            => _instance ??= new StringToImageSourceConverter();
#elif NET472
            => _instance ?? (_instance = new StringToImageSourceConverter());
#endif

    }
}
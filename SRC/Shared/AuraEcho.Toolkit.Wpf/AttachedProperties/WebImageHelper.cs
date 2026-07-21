using AuraEcho.Toolkit.Wpf.Imaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AuraEcho.Toolkit.Wpf.AttachedProperties
{
    public static class WebImageHelper
    {
        /// <summary>
        /// 图片地址
        /// </summary>
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.RegisterAttached(
                "Url",
                typeof(string),
                typeof(WebImageHelper),
                new PropertyMetadata(string.Empty, OnImagePropertyChanged));
        public static string GetUrl(DependencyObject obj) => (string)obj.GetValue(UrlProperty);
        public static void SetUrl(DependencyObject obj, string value) => obj.SetValue(UrlProperty, value);

        /// <summary>
        /// 解码宽度
        /// </summary>
        public static readonly DependencyProperty DecodePixelWidthProperty =
            DependencyProperty.RegisterAttached(
                "DecodePixelWidth",
                typeof(int),
                typeof(WebImageHelper),
                new PropertyMetadata(0, OnImagePropertyChanged));
        public static int GetDecodePixelWidth(DependencyObject obj) => (int)obj.GetValue(DecodePixelWidthProperty);
        public static void SetDecodePixelWidth(DependencyObject obj, int value) => obj.SetValue(DecodePixelWidthProperty, value);

        /// <summary>
        /// 解码高度
        /// </summary>
        public static readonly DependencyProperty DecodePixelHeightProperty =
            DependencyProperty.RegisterAttached(
                "DecodePixelHeight",
                typeof(int),
                typeof(WebImageHelper),
                new PropertyMetadata(0, OnImagePropertyChanged));
        public static int GetDecodePixelHeight(DependencyObject obj) => (int)obj.GetValue(DecodePixelHeightProperty);
        public static void SetDecodePixelHeight(DependencyObject obj, int value) => obj.SetValue(DecodePixelHeightProperty, value);

        /// <summary>
        /// 加载图片
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static async void OnImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Action<BitmapImage> sourceUpdater = null;

            if (d is Image img)
            {
                sourceUpdater = bi => img.Source = bi;
            }
            if (d is ImageBrush ib)
            {
                sourceUpdater = bi => ib.ImageSource = bi;
            }
            if (sourceUpdater is null) return;

            string url = GetUrl(d);
            if (string.IsNullOrWhiteSpace(url))
            {
                sourceUpdater(null);
                return;
            }

            int decodeWidth = GetDecodePixelWidth(d);
            int decodeHeight = GetDecodePixelHeight(d);

            // TODO: 下载中展位图

            var bitmap = await WebImageLoaderContext.Default.GetImageAsync(url, decodeWidth, decodeHeight);

            if (bitmap is null)
            {
                // TODO: 下载失败图
            }

            // 验证在异步等待期间，控件绑定的 URL 是否被修改过（防止列表快速滚动导致图片错乱）
            if (GetUrl(d) == url)
            {
                sourceUpdater(bitmap);
            }
        }
    }
}

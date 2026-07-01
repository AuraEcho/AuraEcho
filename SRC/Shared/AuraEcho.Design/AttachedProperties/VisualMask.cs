using System.Windows;
using System.Windows.Media;

namespace AuraEcho.Design.AttachedProperties
{
    /// <summary>
    /// 为目标元素提供一个附加属性，使其可以使用源元素的视觉内容作为不透明遮罩。
    /// </summary>
    public static class VisualMask
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached(
                "Source",
                typeof(FrameworkElement),
                typeof(VisualMask),
                new PropertyMetadata(null, OnSourceChanged));

        public static FrameworkElement GetSource(FrameworkElement obj) =>
            (FrameworkElement)obj.GetValue(SourceProperty);

        public static void SetSource(FrameworkElement obj, FrameworkElement value) =>
            obj.SetValue(SourceProperty, value);

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (FrameworkElement)d;
            var source = e.NewValue as FrameworkElement;

            if (source is null)
            {
                target.OpacityMask = null;
                return;
            }

            if (source.IsLoaded)
                UpdateMask();

            source.Loaded += OnLoaded;
            source.SizeChanged += OnSizeChanged;

            void OnLoaded(object s, RoutedEventArgs args)
            {
                UpdateMask();
            }

            void OnSizeChanged(object s, SizeChangedEventArgs args)
            {
                UpdateMask();
            }

            void UpdateMask()
            {
                if (source.ActualWidth > 0 && source.ActualHeight > 0)
                {
                    target.OpacityMask = new VisualBrush(source)
                    {
                        Viewbox = new Rect(0, 0, source.ActualWidth, source.ActualHeight),
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewport = new Rect(0, 0, source.ActualWidth, source.ActualHeight),
                        ViewportUnits = BrushMappingMode.Absolute,
                        Stretch = Stretch.None
                    };
                }
            }
        }
    }
}

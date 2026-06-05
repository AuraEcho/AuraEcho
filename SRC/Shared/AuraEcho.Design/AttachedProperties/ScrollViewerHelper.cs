using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Design.AttachedProperties
{
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty VerticalScrollBarMarginProperty
            = DependencyProperty.RegisterAttached("VerticalScrollBarMargin", typeof(Thickness), typeof(ScrollViewerHelper), new UIPropertyMetadata(new Thickness(0)));
        public static void SetVerticalScrollBarMargin(FrameworkElement target, Thickness value)
            => target.SetValue(VerticalScrollBarMarginProperty, value);
        public static Thickness GetVerticalScrollBarMargin(FrameworkElement target)
            => (Thickness)target.GetValue(VerticalScrollBarMarginProperty);

        public static readonly DependencyProperty HorizontalScrollBarMarginProperty
            = DependencyProperty.RegisterAttached("HorizontalScrollBarMargin", typeof(Thickness), typeof(ScrollViewerHelper), new UIPropertyMetadata(new Thickness(0)));
        public static void SetHorizontalScrollBarMargin(FrameworkElement target, Thickness value)
            => target.SetValue(HorizontalScrollBarMarginProperty, value);
        public static Thickness GetHorizontalScrollBarMargin(FrameworkElement target)
            => (Thickness)target.GetValue(HorizontalScrollBarMarginProperty);

        public static readonly DependencyProperty HorizontalOffsetProperty 
            = DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollViewerHelper), new UIPropertyMetadata(0.0, OnHorizontalOffsetChanged));
        public static void SetHorizontalOffset(FrameworkElement target, double value) 
            => target.SetValue(HorizontalOffsetProperty, value);
        public static double GetHorizontalOffset(FrameworkElement target) 
            => (double)target.GetValue(HorizontalOffsetProperty);
        private static void OnHorizontalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) 
            => (target as ScrollViewer)?.ScrollToHorizontalOffset((double)e.NewValue);

        public static readonly DependencyProperty VerticalOffsetProperty 
            = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerHelper), new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));
        public static void SetVerticalOffset(FrameworkElement target, double value) 
            => target.SetValue(VerticalOffsetProperty, value);
        public static double GetVerticalOffset(FrameworkElement target) 
            => (double)target.GetValue(VerticalOffsetProperty);
        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) 
            => (target as ScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue);
    }
}

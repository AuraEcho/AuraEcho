using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Design.Controls
{
    /// <summary>
    /// Skeleton 占位控件
    /// </summary>
    public class SkeletonRectangle : Control
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SkeletonRectangle),
                new PropertyMetadata(new CornerRadius(2)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        static SkeletonRectangle()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SkeletonRectangle),
                new FrameworkPropertyMetadata(typeof(SkeletonRectangle)));
        }
    }
}

using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Shapes;

namespace AuraEcho.Toolkit.Wpf.Behaviors
{
    public class ShapeClipBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty SourceShapeProperty =
            DependencyProperty.Register(nameof(SourceShape), typeof(Shape), typeof(ShapeClipBehavior),
                new PropertyMetadata(null, OnSourceShapeChanged));

        public Shape SourceShape
        {
            get => (Shape)GetValue(SourceShapeProperty);
            set => SetValue(SourceShapeProperty, value);
        }

        private static void OnSourceShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ShapeClipBehavior)d;

            if (e.OldValue is Shape oldShape)
            {
                oldShape.SizeChanged -= behavior.OnShapeSizeChanged;
            }

            if (e.NewValue is Shape newShape)
            {
                newShape.SizeChanged += behavior.OnShapeSizeChanged;
                behavior.UpdateClip();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateClip();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (SourceShape is null) return;

            SourceShape.SizeChanged -= OnShapeSizeChanged;
        }

        private void OnShapeSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClip();
        }

        private void UpdateClip()
        {
            if (SourceShape is null) return;

            if (AssociatedObject is null) return;

            AssociatedObject.Clip = SourceShape.RenderedGeometry;
        }
    }
}

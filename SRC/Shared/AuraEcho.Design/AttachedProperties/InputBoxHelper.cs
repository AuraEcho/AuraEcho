using System.Windows;
using System.Windows.Media;

namespace AuraEcho.Design.AttachedProperties
{
    public class InputBoxHelper
    {
        public static readonly DependencyProperty FloatingBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "FloatingBackground",
                typeof(Brush),
                typeof(InputBoxHelper),
                new PropertyMetadata(default(Brush)));

        public static void SetFloatingBackground(
            DependencyObject element,
            Brush value)
            => element.SetValue(FloatingBackgroundProperty, value);
        public static Brush GetFloatingBackground(
            DependencyObject element)
            => element.GetValue(FloatingBackgroundProperty) as Brush;
    }
}
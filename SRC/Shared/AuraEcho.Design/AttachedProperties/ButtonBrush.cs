using System.Windows;
using System.Windows.Media;

namespace AuraEcho.Design.AttachedProperties
{
    public class ButtonBrush
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.RegisterAttached(
                "Background",
                typeof(Brush),
                typeof(ButtonBrush),
                new PropertyMetadata(default(Brush)));
        public static void SetBackground(
            DependencyObject element,
            Brush value)
            => element.SetValue(BackgroundProperty, value);
        public static Brush GetBackground(
            DependencyObject element)
            => element.GetValue(BackgroundProperty) as Brush;

        public static readonly DependencyProperty PressBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "PressBackground",
                typeof(Brush),
                typeof(ButtonBrush),
                new PropertyMetadata(default(Brush)));
        public static void SetPressBackground(
            DependencyObject element,
            Brush value)
            => element.SetValue(PressBackgroundProperty, value);
        public static Brush GetPressBackground(
            DependencyObject element)
            => element.GetValue(PressBackgroundProperty) as Brush;

        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "HoverBackground",
                typeof(Brush),
                typeof(ButtonBrush),
                new PropertyMetadata(default(Brush)));
        public static void SetHoverBackground(
            DependencyObject element,
            Brush value)
            => element.SetValue(HoverBackgroundProperty, value);
        public static Brush GetHoverBackground(
            DependencyObject element)
            => element.GetValue(HoverBackgroundProperty) as Brush;
    }
}

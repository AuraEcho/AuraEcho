using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace AuraEcho.UIToolkit.Behaviors
{
    /// <summary>
    /// 点击空白处清除焦点的行为
    /// </summary>
    public class ClearFocusOnClickBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            AssociatedObject.MouseDown += AssociatedObject_MouseDown;
            base.OnAttached();
        }

        private static void AssociatedObject_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseDown -= AssociatedObject_MouseDown;
        }
    }
}

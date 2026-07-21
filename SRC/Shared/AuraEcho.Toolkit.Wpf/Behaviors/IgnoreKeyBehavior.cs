using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace AuraEcho.Toolkit.Wpf.Behaviors
{
    public class IgnoreKeyBehavior : Behavior<UIElement>
    {
        public Key Key { get; set; }
        protected override void OnAttached()
        {
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key;
        }
    }
}

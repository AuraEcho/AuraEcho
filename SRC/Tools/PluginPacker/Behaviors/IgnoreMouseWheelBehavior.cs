using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PluginPacker.Behaviors;

public class IgnoreMouseWheelBehavior : Behavior<UIElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += AssociatedObjectPreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= AssociatedObjectPreviewMouseWheel;
        base.OnDetaching();
    }

    private void AssociatedObjectPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        if (AssociatedObject is ComboBox cb)
        {
            e.Handled = !cb.IsDropDownOpen;
        }

        var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
        e2.RoutedEvent = UIElement.MouseWheelEvent;
        AssociatedObject.RaiseEvent(e2);
    }
}

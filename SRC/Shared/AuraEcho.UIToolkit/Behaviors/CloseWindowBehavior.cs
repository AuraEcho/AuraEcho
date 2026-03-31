using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace AuraEcho.UIToolkit.Behaviors
{
    /// <summary>
    /// 关闭窗口行为
    /// </summary>
    public class CloseWindowBehavior : Behavior<Window>
    {
        public bool CloseTrigger
        {
            get => (bool)GetValue(CloseTriggerProperty);
            set => SetValue(CloseTriggerProperty, value);
        }

        public static readonly DependencyProperty CloseTriggerProperty
            = DependencyProperty.Register(
                "CloseTrigger",
                typeof(bool),
                typeof(CloseWindowBehavior),
                new PropertyMetadata(false, OnCloseTriggerChanged));

        private static void OnCloseTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is CloseWindowBehavior cwb)) return;

            cwb.OnCloseTriggerChanged();
        }

        private void OnCloseTriggerChanged()
        {
            if (!CloseTrigger) return;

            this.AssociatedObject.Close();
        }
    }
}

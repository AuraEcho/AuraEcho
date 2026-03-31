using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace AuraEcho.UIToolkit.Behaviors
{
    /// <summary>
    /// 拖放命令行为，用于在拖放操作时执行指定的命令。
    /// </summary>
    public class DropCommandBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty CommandProperty
            = DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(DropCommandBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Drop -= OnDrop;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (Command?.CanExecute(e) == true)
            {
                Command.Execute(e);
            }
        }
    }
}

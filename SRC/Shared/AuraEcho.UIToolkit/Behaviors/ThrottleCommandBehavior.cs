using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AuraEcho.UIToolkit.Behaviors
{
    public class ThrottleCommandBehavior : Behavior<Button>
    {
        private DispatcherTimer _timer;
        private bool _isThrottling;

        public static readonly DependencyProperty IntervalMillisecondsProperty =
            DependencyProperty.Register(
                nameof(IntervalMilliseconds),
                typeof(int),
                typeof(ThrottleCommandBehavior),
                new PropertyMetadata(500));

        public int IntervalMilliseconds
        {
            get => (int)GetValue(IntervalMillisecondsProperty);
            set => SetValue(IntervalMillisecondsProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(ThrottleCommandBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(ThrottleCommandBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;

            AssociatedObject.Click += OnButtonClick;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (_isThrottling) return;

            if (Command?.CanExecute(null) == true)
            {
                Command.Execute(CommandParameter);
            }

            _isThrottling = true;
            _timer.Interval = TimeSpan.FromMilliseconds(IntervalMilliseconds);
            _timer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            _isThrottling = false; // 冷却结束
        }

        protected override void OnDetaching()
        {
            if (_timer != null)
            {
                _timer.Tick -= OnTimerTick;
                _timer.Stop();
                _timer = null;
            }
            AssociatedObject.Click -= OnButtonClick;
            base.OnDetaching();
        }
    }
}

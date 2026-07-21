using System;
using System.Windows;
using AuraEcho.Toolkit.Wpf.EntranceEffects;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.Toolkit.Wpf.Behaviors
{
    /// <summary>
    /// 当目标元素 <see cref="FrameworkElement.Loaded"/> 时，对其施加入场动画。
    /// </summary>
    public class LoadedEntranceAnimationBehavior : Behavior<FrameworkElement>
    {
        #region EntranceEffect

        public EntranceEffectBase EntranceEffect
        {
            get => (EntranceEffectBase)GetValue(EntranceEffectProperty);
            set => SetValue(EntranceEffectProperty, value);
        }

        public static readonly DependencyProperty EntranceEffectProperty =
            DependencyProperty.Register(
                nameof(EntranceEffect),
                typeof(EntranceEffectBase),
                typeof(LoadedEntranceAnimationBehavior),
                new PropertyMetadata(new SlideFadeEntranceEffect()));

        #endregion

        #region Delay

        public TimeSpan Delay
        {
            get => (TimeSpan)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register(
                nameof(Delay),
                typeof(TimeSpan),
                typeof(LoadedEntranceAnimationBehavior),
                new PropertyMetadata(TimeSpan.Zero));

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject.IsLoaded)
            {
                AnimateElement(AssociatedObject);
                return;
            }

            AssociatedObject.Loaded += OnElementLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnElementLoaded;
            base.OnDetaching();
        }

        private void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            element.Loaded -= OnElementLoaded;

            AnimateElement(element);
        }

        private void AnimateElement(FrameworkElement element)
        {
            EntranceEffect?.ApplyTo(element, index: 1, staggerDelay: Delay);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AuraEcho.UIToolkit.EntranceEffects;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.UIToolkit.Behaviors
{
    /// <summary>
    /// ItemsControl 子项依次错落入场的行为。
    /// 通过 <see cref="EntranceEffect"/> 属性可配置预设或自定义入场动画效果。
    /// </summary>
    public class EntranceAnimationBehavior : Behavior<ItemsControl>
    {
        #region 私有附加属性

        /// <summary>标记项容器是否已执行过入场动画</summary>
        private static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.RegisterAttached(
                "IsAnimated",
                typeof(bool),
                typeof(EntranceAnimationBehavior),
                new PropertyMetadata(false));

        private static bool GetIsAnimated(DependencyObject obj) => (bool)obj.GetValue(IsAnimatedProperty);
        private static void SetIsAnimated(DependencyObject obj) => obj.SetValue(IsAnimatedProperty, true);

        #endregion

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
                typeof(EntranceAnimationBehavior),
                new PropertyMetadata(new SlideFadeEntranceEffect()));

        #endregion

        #region StaggerDelay

        public TimeSpan StaggerDelay
        {
            get => (TimeSpan)GetValue(StaggerDelayProperty);
            set => SetValue(StaggerDelayProperty, value);
        }

        public static readonly DependencyProperty StaggerDelayProperty =
            DependencyProperty.Register(
                nameof(StaggerDelay),
                typeof(TimeSpan),
                typeof(EntranceAnimationBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(50)));

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.ItemContainerGenerator.StatusChanged += OnStatusChanged;

            if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                DiscoverContainers();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ItemContainerGenerator.StatusChanged -= OnStatusChanged;

            for (var i = 0; i < AssociatedObject.Items.Count; i++)
            {
                if (AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
                    container.Loaded -= OnContainerLoaded;
            }

            base.OnDetaching();
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                DiscoverContainers();
        }

        private void DiscoverContainers()
        {
            if (AssociatedObject.Items.Count == 0) return;

            for (var i = 0; i < AssociatedObject.Items.Count; i++)
            {
                var container = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null || GetIsAnimated(container))
                    continue;

                SetIsAnimated(container);

                if (container.IsLoaded)
                    AnimateContainer(container);
                else
                    container.Loaded += OnContainerLoaded;
            }
        }

        private void OnContainerLoaded(object sender, RoutedEventArgs e)
        {
            var container = (FrameworkElement)sender;
            container.Loaded -= OnContainerLoaded;

            AnimateContainer(container);
        }

        private void AnimateContainer(FrameworkElement container)
        {
            var index = AssociatedObject.ItemContainerGenerator.IndexFromContainer(container);
            if (index < 0) return;

            EntranceEffect?.ApplyTo(container, index, StaggerDelay);
        }
    }
}

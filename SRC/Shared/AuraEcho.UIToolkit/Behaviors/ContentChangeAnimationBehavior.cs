using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AuraEcho.UIToolkit.EntranceEffects;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.UIToolkit.Behaviors
{
    /// <summary>
    /// 当 <see cref="ContentControl"/> 的内容发生变化时，对新内容施加入场动画。
    /// </summary>
    public class ContentChangeAnimationBehavior : Behavior<ContentControl>
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
                typeof(ContentChangeAnimationBehavior),
                new PropertyMetadata(new SlideFadeEntranceEffect()));

        #endregion

        private static readonly DependencyPropertyDescriptor ContentDescriptor =
            DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));

        protected override void OnAttached()
        {
            base.OnAttached();
            ContentDescriptor.AddValueChanged(AssociatedObject, OnContentChanged);
        }

        protected override void OnDetaching()
        {
            ContentDescriptor.RemoveValueChanged(AssociatedObject, OnContentChanged);
            base.OnDetaching();
        }

        private void OnContentChanged(object sender, EventArgs e)
        {
            if (AssociatedObject.Content is FrameworkElement content)
            {
                if (content.IsLoaded)
                {
                    AnimateContent(content);
                    return;
                }

                content.Loaded += OnNewContentLoaded;
            }
        }

        private void OnNewContentLoaded(object sender, RoutedEventArgs e)
        {
            var content = (FrameworkElement)sender;
            content.Loaded -= OnNewContentLoaded;

            AnimateContent(content);
        }

        private void AnimateContent(FrameworkElement content)
        {
            EntranceEffect?.ApplyTo(content, index: 0, staggerDelay: TimeSpan.Zero);
        }
    }
}

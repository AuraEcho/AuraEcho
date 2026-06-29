using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AuraEcho.UIToolkit.EntranceEffects
{
    /// <summary>
    /// 入场动画效果抽象基类。
    /// </summary>
    public abstract class EntranceEffectBase
    {
        #region Transform 附加/移除基础设施

        /// <summary>标记元素的 TransformGroup 是否由此类创建</summary>
        private static readonly DependencyProperty HasTransformWrapperProperty =
            DependencyProperty.RegisterAttached(
                "HasTransformWrapper",
                typeof(bool),
                typeof(EntranceEffectBase),
                new PropertyMetadata(false));

        private static bool GetHasTransformWrapper(DependencyObject obj) 
            => (bool)obj.GetValue(HasTransformWrapperProperty);

        private static void SetHasTransformWrapper(DependencyObject obj, bool value) 
            => obj.SetValue(HasTransformWrapperProperty, value);

        protected static string AttachTransform(FrameworkElement element, Transform animTransform)
        {
            var existing = element.RenderTransform;
            string prefix;

            if (existing is TransformGroup existingGroup)
            {
                existingGroup.Children.Add(animTransform);
                var index = existingGroup.Children.Count - 1;
                prefix = $"(UIElement.RenderTransform).(TransformGroup.Children)[{index}]";
            }
            else
            {
                var group = new TransformGroup();
                group.Children.Add(existing);       // [0] — 原变换（可能为 Identity）
                group.Children.Add(animTransform);  // [1] — 动画变换
                element.RenderTransform = group;
                prefix = "(UIElement.RenderTransform).(TransformGroup.Children)[1]";
                SetHasTransformWrapper(element, true);
            }

            return prefix;
        }

        protected static void RemoveTransform(FrameworkElement element)
        {
            if (element.RenderTransform is TransformGroup group)
            {
                if (GetHasTransformWrapper(element))
                {
                    element.RenderTransform = group.Children[0];
                    SetHasTransformWrapper(element, false);
                }
                else if (group.Children.Count > 0)
                {
                    group.Children.RemoveAt(group.Children.Count - 1);
                }
            }
        }

        #endregion

        #region 公共入口

        /// <summary>
        /// 对目标元素施加入场效果。
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="index">在列表中的索引，用于计算交错延迟</param>
        /// <param name="staggerDelay">相邻元素间的交错间隔</param>
        public void ApplyTo(FrameworkElement element, int index, TimeSpan staggerDelay)
        {
            if (element == null) return;

            var delay = TimeSpan.FromTicks(staggerDelay.Ticks * Math.Max(0, index));

            var storyboard = Build(element, delay);
            if (storyboard == null || storyboard.Children.Count == 0) return;

            storyboard.Completed += (_, __) => RemoveTransform(element);
            storyboard.Begin(element, true);
        }

        /// <summary>
        /// 创建入场效果 Storyboard
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="delay">本元素相对交错起点的延迟</param>
        /// <returns>准备就绪的 Storyboard，或 null</returns>
        protected abstract Storyboard Build(FrameworkElement element, TimeSpan delay);
        #endregion
    }
}

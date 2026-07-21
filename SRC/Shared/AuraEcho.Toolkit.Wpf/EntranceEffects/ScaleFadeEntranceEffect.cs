using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AuraEcho.Toolkit.Wpf.EntranceEffects
{
    /// <summary>
    /// 缩放淡入入场效果
    /// </summary>
    public class ScaleFadeEntranceEffect : EntranceEffectBase
    {
        public Duration Duration { get; set; } = new Duration(TimeSpan.FromMilliseconds(400));

        public IEasingFunction Easing { get; set; } = new CubicEase { EasingMode = EasingMode.EaseOut };

        public double FromScale { get; set; } = 0.92;

        protected override Storyboard Build(FrameworkElement element, TimeSpan delay)
        {
            var run = Duration.HasTimeSpan ? Duration.TimeSpan : TimeSpan.FromMilliseconds(400);
            var storyboard = new Storyboard();

            // 淡入
            storyboard.Children.Add(CreatePropertyAnimation(element, UIElement.OpacityProperty, 0, 1, delay, run));

            // 缩放 —— 以元素中心为锚点
            var scale = new ScaleTransform(FromScale, FromScale)
            {
                CenterX = element.ActualWidth / 2,
                CenterY = element.ActualHeight / 2
            };
            var prefix = AttachTransform(element, scale);

            storyboard.Children.Add(CreateTransformAnimation(element, prefix, nameof(ScaleTransform.ScaleX), FromScale, 1, delay, run));
            storyboard.Children.Add(CreateTransformAnimation(element, prefix, nameof(ScaleTransform.ScaleY), FromScale, 1, delay, run));

            return storyboard;
        }

        private DoubleAnimationUsingKeyFrames CreateStaggeredAnimation(
            double from, double to, TimeSpan delay, TimeSpan run)
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(delay + run),
                FillBehavior = FillBehavior.Stop
            };

            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));

            if (delay > TimeSpan.Zero)
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(delay)));

            animation.KeyFrames.Add(new EasingDoubleKeyFrame(to, KeyTime.FromTimeSpan(delay + run), Easing));

            return animation;
        }

        private Timeline CreatePropertyAnimation(FrameworkElement target, DependencyProperty property,
            double from, double to, TimeSpan delay, TimeSpan run)
        {
            var anim = CreateStaggeredAnimation(from, to, delay, run);
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            return anim;
        }

        private Timeline CreateTransformAnimation(FrameworkElement target, string prefix,
            string propertyName, double from, double to, TimeSpan delay, TimeSpan run)
        {
            var anim = CreateStaggeredAnimation(from, to, delay, run);
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath($"{prefix}.({propertyName})"));
            return anim;
        }
    }
}

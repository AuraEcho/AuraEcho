using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AuraEcho.UIToolkit.EntranceEffects
{
    /// <summary>
    /// 滑动淡入入场效果
    /// </summary>
    public class SlideFadeEntranceEffect : EntranceEffectBase
    {
        public Duration Duration { get; set; } = new Duration(TimeSpan.FromMilliseconds(400));

        public IEasingFunction Easing { get; set; } = new CubicEase { EasingMode = EasingMode.EaseOut };

        public SlideDirection Direction { get; set; } = SlideDirection.Up;

        public double Offset { get; set; } = 24;

        protected override Storyboard Build(FrameworkElement element, TimeSpan delay)
        {
            var run = Duration.HasTimeSpan ? Duration.TimeSpan : TimeSpan.FromMilliseconds(400);
            var storyboard = new Storyboard();

            // 淡入
            storyboard.Children.Add(CreatePropertyAnimation(element, UIElement.OpacityProperty, 0, 1, delay, run));

            // 滑动
            GetOffset(out var fromX, out var fromY);
            var translate = new TranslateTransform();
            var prefix = AttachTransform(element, translate);

            if (Math.Abs(fromX) > 0.001)
                storyboard.Children.Add(CreateTransformAnimation(element, prefix, nameof(TranslateTransform.X), fromX, 0, delay, run));

            if (Math.Abs(fromY) > 0.001)
                storyboard.Children.Add(CreateTransformAnimation(element, prefix, nameof(TranslateTransform.Y), fromY, 0, delay, run));

            return storyboard;
        }

        private void GetOffset(out double x, out double y)
        {
            x = 0; y = 0;
            switch (Direction)
            {
                case SlideDirection.Up: y = Offset;  break;
                case SlideDirection.Down: y = -Offset; break;
                case SlideDirection.Left: x = Offset;  break;
                case SlideDirection.Right: x = -Offset; break;
            }
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

        private DoubleAnimationUsingKeyFrames CreatePropertyAnimation(
            FrameworkElement target, 
            DependencyProperty property,
            double from, 
            double to, 
            TimeSpan delay, 
            TimeSpan run)
        {
            var anim = CreateStaggeredAnimation(from, to, delay, run);
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            return anim;
        }

        private DoubleAnimationUsingKeyFrames CreateTransformAnimation(
            FrameworkElement target, 
            string prefix,
            string propertyName, 
            double from, 
            double to, 
            TimeSpan delay, 
            TimeSpan run)
        {
            var anim = CreateStaggeredAnimation(from, to, delay, run);
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath($"{prefix}.({propertyName})"));
            return anim;
        }
    }
}

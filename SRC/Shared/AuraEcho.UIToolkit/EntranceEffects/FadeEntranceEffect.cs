using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace AuraEcho.UIToolkit.EntranceEffects
{
    /// <summary>
    /// 淡入入场效果
    /// </summary>
    public class FadeEntranceEffect : EntranceEffectBase
    {
        public Duration Duration { get; set; } = new Duration(TimeSpan.FromMilliseconds(400));

        public IEasingFunction Easing { get; set; } = new CubicEase { EasingMode = EasingMode.EaseOut };

        protected override Storyboard Build(FrameworkElement element, TimeSpan delay)
        {
            var run = Duration.HasTimeSpan ? Duration.TimeSpan : TimeSpan.FromMilliseconds(400);
            var storyboard = new Storyboard();

            var fade = CreateStaggeredAnimation(0, 1, delay, run);
            Storyboard.SetTarget(fade, element);
            Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fade);

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
    }
}

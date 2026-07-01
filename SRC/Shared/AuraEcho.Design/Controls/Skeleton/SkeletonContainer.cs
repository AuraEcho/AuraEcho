using System;
using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Design.Controls
{
    /// <summary>
    /// 骨架屏容器
    /// </summary>
    [TemplatePart(Name = PartRealContent, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartSkeletonContent, Type = typeof(ContentPresenter))]
    public class SkeletonContainer : ContentControl
    {
        private const string PartRealContent = "PART_RealContent";
        private const string PartSkeletonContent = "PART_SkeletonContent";

        private ContentPresenter _realContent;
        private ContentPresenter _skeletonContent;

        #region Dependency Properties

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(SkeletonContainer),
                new PropertyMetadata(false, OnIsLoadingChanged));

        public static readonly DependencyProperty SkeletonProperty =
            DependencyProperty.Register(
                nameof(Skeleton),
                typeof(object),
                typeof(SkeletonContainer));

        public static readonly DependencyProperty TransitionDurationProperty =
            DependencyProperty.Register(
                nameof(TransitionDuration),
                typeof(Duration),
                typeof(SkeletonContainer),
                new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200))));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public object Skeleton
        {
            get => GetValue(SkeletonProperty);
            set => SetValue(SkeletonProperty, value);
        }

        public Duration TransitionDuration
        {
            get => (Duration)GetValue(TransitionDurationProperty);
            set => SetValue(TransitionDurationProperty, value);
        }

        #endregion

        static SkeletonContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SkeletonContainer),
                new FrameworkPropertyMetadata(typeof(SkeletonContainer)));
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var container = (SkeletonContainer)d;
            container.UpdateState();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _realContent = GetTemplateChild(PartRealContent) as ContentPresenter;
            _skeletonContent = GetTemplateChild(PartSkeletonContent) as ContentPresenter;

            UpdateState();
        }

        private void UpdateState()
        {
            VisualStateManager.GoToState(this, IsLoading ? "Loading" : "Loaded", true);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.Toolkit.Wpf.Behaviors
{
    /// <summary>
    /// 为任意 <see cref="FrameworkElement"/> 附加加载动画遮罩的行为。
    /// </summary>
    public class LoadingOverlayBehavior : Behavior<FrameworkElement>
    {
        #region IsLoading

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public Brush Overlay
        {
            get => (Brush)GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }
        public static readonly DependencyProperty OverlayProperty =
            DependencyProperty.Register(
                nameof(Overlay),
                typeof(Brush),
                typeof(LoadingOverlayBehavior),
                new PropertyMetadata(null, OnOverlayChanged));

        private static void OnOverlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LoadingOverlayBehavior behavior && behavior._adorner != null)
            {
                behavior._adorner.Background = e.NewValue as Brush;
            }
        }

        public CornerRadius CornerRadius { get; set; }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(LoadingOverlayBehavior),
                new PropertyMetadata(false, OnIsLoadingChanged));

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (LoadingOverlayBehavior)d;

            if (behavior.AssociatedObject is null) return;

            if ((bool)e.NewValue)
                behavior.TryShowOverlay();
            else
                behavior.HideOverlay();
        }

        #endregion

        #region LoadingContent

        public string LoadingContent
        {
            get => (string)GetValue(LoadingContentProperty);
            set => SetValue(LoadingContentProperty, value);
        }

        public static readonly DependencyProperty LoadingContentProperty =
            DependencyProperty.Register(
                nameof(LoadingContent),
                typeof(string),
                typeof(LoadingOverlayBehavior),
                new PropertyMetadata(string.Empty));

        #endregion

        private LoadingAdorner _adorner;
        private bool _pendingShow;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded += OnAssociatedObjectUnloaded;

            if (AssociatedObject.IsLoaded && IsLoading)
                TryShowOverlay();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded -= OnAssociatedObjectUnloaded;

            HideOverlay();

            base.OnDetaching();
        }

        private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            if (_pendingShow || IsLoading)
            {
                _pendingShow = false;
                ShowOverlay();
            }
        }

        private void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            HideOverlay();
            _pendingShow = IsLoading;
        }

        private void TryShowOverlay()
        {
            if (!IsLoading) return;

            if (AssociatedObject.IsLoaded)
                ShowOverlay();
            else
                _pendingShow = true;
        }

        private void ShowOverlay()
        {
            if (_adorner != null) return;

            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            if (layer == null)
            {
                _pendingShow = true;
                return;
            }

            _adorner = new LoadingAdorner(AssociatedObject)
            {
                LoadingContent = LoadingContent,
                CornerRadius = CornerRadius,
                Background = Overlay
            };

            layer.Add(_adorner);
            _adorner.StartAnimation();
        }

        private void HideOverlay()
        {
            if (_adorner == null) return;

            _adorner.StopAnimation();

            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            layer?.Remove(_adorner);

            _adorner = null;
            _pendingShow = false;
        }

        /// <summary>
        /// Loading 遮罩装饰器
        /// </summary>
        private sealed class LoadingAdorner : Adorner
        {
            private readonly Border _overlay;
            private readonly Ellipse _spinner;
            private readonly TextBlock _loadingText;
            private Storyboard _animation;

            private Brush _background;
            public Brush Background 
            {
                get => _background;
                set
                {
                    _background = value;
                    _overlay.Background = _background;
                }
            }

            private CornerRadius _cornerRadius;
            public CornerRadius CornerRadius 
            {
                get => _cornerRadius;
                set
                {
                    _cornerRadius = value;
                    _overlay.CornerRadius = value;
                }
            }

            public LoadingAdorner(UIElement adornedElement) : base(adornedElement)
            {
                // 旋转圆环
                _spinner = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    StrokeDashArray = new DoubleCollection(new[] { 20.0, 40.0 }),
                    StrokeThickness = 2,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(),
                    Stroke = new SolidColorBrush(Colors.White)
                };

                // 加载文字
                _loadingText = new TextBlock
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                };
                _loadingText.SetResourceReference(TextBlock.ForegroundProperty, "Brushes.OnSurface");

                var contentStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Orientation = Orientation.Horizontal
                };
                contentStack.Children.Add(_spinner);
                contentStack.Children.Add(_loadingText);

                // 半透明遮罩
                _overlay = new Border
                {
                    Child = contentStack,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true,
                    CornerRadius = CornerRadius
                };

                if (Background is null)
                {
                    _overlay.SetResourceReference(Border.BackgroundProperty, "Brushes.Scrim");
                }
                else
                {
                    _overlay.Background = Background;
                }

                AddVisualChild(_overlay);
                AddLogicalChild(_overlay);
            }

            public string LoadingContent
            {
                get => _loadingText.Text;
                set => _loadingText.Text = value;
            }

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                return _overlay;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _overlay.Arrange(new Rect(finalSize));
                return finalSize;
            }

            public void StartAnimation()
            {
                StopAnimation();

                _animation = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 圆环虚线偏移动画
                var dashAnimation = new DoubleAnimationUsingKeyFrames();
                dashAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                dashAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(20, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(700))));
                dashAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400))));
                Storyboard.SetTarget(dashAnimation, _spinner);
                Storyboard.SetTargetProperty(dashAnimation, new PropertyPath(Shape.StrokeDashOffsetProperty));
                _animation.Children.Add(dashAnimation);

                // 旋转动画
                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 720,
                    Duration = TimeSpan.FromMilliseconds(1400)
                };
                Storyboard.SetTarget(rotateAnimation, _spinner);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
                _animation.Children.Add(rotateAnimation);

                _animation.Begin(_overlay, true);
            }

            public void StopAnimation()
            {
                if (_animation == null) return;

                _animation.Stop(_overlay);
                _animation.Remove(_overlay);
                _animation = null;
            }
        }
    }
}

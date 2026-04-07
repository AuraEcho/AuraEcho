using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace AuraEcho.Views;

/// <summary>
/// Interaction logic for MarketplacePluginDetails
/// </summary>
public partial class MarketplacePluginDetails : UserControl
{
    private readonly Storyboard _screenshotStoryboard;
    private readonly DoubleAnimation _scrollAnimation;

    public MarketplacePluginDetails()
    {
        InitializeComponent();
        _screenshotStoryboard = (Storyboard)FindResource("ScreenshotStoryboard");
        _scrollAnimation = (DoubleAnimation)_screenshotStoryboard.Children[0];
    }

    private void ScrollLeft_Click(object sender, RoutedEventArgs e)
    {
        DoScroll(-488);
    }

    private void ScrollRight_Click(object sender, RoutedEventArgs e)
    {
        DoScroll(488);
    }

    private void DoScroll(double offsetDelta)
    {
        double targetOffset = ScreenshotScrollViewer.HorizontalOffset + offsetDelta;

        targetOffset = Math.Max(0, Math.Min(targetOffset, ScreenshotScrollViewer.ScrollableWidth));
        _scrollAnimation.To = targetOffset;

        _screenshotStoryboard.Begin();
    }

    private void ScreenshotScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        ScrollLeftButton.Visibility = 
            e.HorizontalOffset > 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;

        ScrollRightButton.Visibility = 
            e.HorizontalOffset >= ScreenshotScrollViewer.ScrollableWidth 
            ? Visibility.Collapsed 
            : Visibility.Visible;
    }

    private void Button_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        => e.Handled = true;
}

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AuraEcho.Views;

/// <summary>
/// Interaction logic for SignIn
/// </summary>
public partial class SignIn : UserControl
{
    private Storyboard ToCodeSignStoryboard => (Storyboard)SignModeLayout.FindResource("ToCodeButton");
    private DoubleAnimation ToCodeSignAnimation => (DoubleAnimation)ToCodeSignStoryboard.Children[0];

    private Storyboard ToPasswordSignStoryboard => (Storyboard)SignModeLayout.FindResource("ToPasswordButton");
    private DoubleAnimation ToPasswordAnimation => (DoubleAnimation)ToPasswordSignStoryboard.Children[0];

    public SignIn()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        double codeModeOffsetX = VisualTreeHelper.GetOffset(CodeModeRadioButton).X;
        double codeModeButtonCenterX = codeModeOffsetX + CodeModeRadioButton.ActualWidth / 2;
        ToCodeSignAnimation.To = codeModeButtonCenterX - SignInModeBg.ActualWidth / 2;

        double passwordModeOffsetX = VisualTreeHelper.GetOffset(PasswordModeRadioButton).X;
        double passwordModeButtonCenterX = passwordModeOffsetX + PasswordModeRadioButton.ActualWidth / 2;
        ToPasswordAnimation.To = passwordModeButtonCenterX - SignInModeBg.ActualWidth / 2;
    }
}

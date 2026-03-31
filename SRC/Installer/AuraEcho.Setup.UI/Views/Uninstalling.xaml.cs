using AuraEcho.Setup.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AuraEcho.Setup.UI.Views
{
    /// <summary>
    /// Interaction logic for Uninstalling
    /// </summary>
    public partial class Uninstalling : UserControl
    {
        public Uninstalling()
        {
            InitializeComponent();
        }

        private Storyboard _progressStoryboard => (Storyboard)FindResource("ProgressStorybard");

        public void BeginProgressStoryboard()
        {
            var newProgress = (DataContext as UninstallingViewModel).Progress;
            _progressStoryboard.Begin();
            UninstallProgressBar.Value = newProgress;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _progressStoryboard.Begin();
            _progressStoryboard.Stop();
        }
    }
}

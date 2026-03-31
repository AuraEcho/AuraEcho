using System.Windows;

namespace AuraEcho.Themes
{
    public partial class DarkTheme : ResourceDictionary
    {
        public DarkTheme() => InitializeComponent();
        public static DarkTheme Instance { get; } = new DarkTheme();
    }
}

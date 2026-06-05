using System.Windows;

namespace AuraEcho.Design.Themes
{
    public partial class LightTheme : ResourceDictionary
    {
        public LightTheme() => InitializeComponent();
        public static LightTheme Instance { get; } = new LightTheme();
    }
}

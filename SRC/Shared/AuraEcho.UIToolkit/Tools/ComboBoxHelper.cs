using System.Windows.Controls;

namespace AuraEcho.UIToolkit.Tools
{
    public static class ComboBoxHelper
    {
        public static void RefreshItems(ComboBox comboBox)
        {
            int i = comboBox.SelectedIndex;
            comboBox.SelectedIndex = -1;
            comboBox.SelectedIndex = i;
            comboBox.Items.Refresh();
        }
    }
}

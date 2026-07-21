using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AuraEcho.Toolkit.Wpf.Behaviors
{
    /// <summary>
    /// 数字输入框行为，限制输入为整数
    /// </summary>
    public class Int32TextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.PreviewTextInput += TextBox_PreviewTextInput;
            AssociatedObject.PreviewKeyDown += TextBox_PreviewKeyDown;
            DataObject.AddPastingHandler(AssociatedObject, TextBoxPasting);
            AssociatedObject.SetValue(InputMethod.IsInputMethodEnabledProperty, false);
        }
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)) ||
                !Int32.TryParse((string)e.DataObject.GetData(typeof(string)), out _))
            {
                e.CancelCommand();
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Int32.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTextInput -= TextBox_PreviewTextInput;
            AssociatedObject.PreviewKeyDown -= TextBox_PreviewKeyDown;
            DataObject.RemovePastingHandler(AssociatedObject, TextBoxPasting);
            AssociatedObject.SetValue(InputMethod.IsInputMethodEnabledProperty, true);
        }
    }
}

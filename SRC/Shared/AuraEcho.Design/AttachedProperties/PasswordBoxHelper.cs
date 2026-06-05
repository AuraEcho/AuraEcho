using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Design.AttachedProperties
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(
                "Password",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnAttachPropertyChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordBoxHelper));


        public static readonly DependencyProperty PasswordLengthProperty =
            DependencyProperty.RegisterAttached(
                "PasswordLength",
                typeof(int),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(0));

        public static int GetPasswordLength(DependencyObject obj) => (int)obj.GetValue(PasswordLengthProperty);
        public static void SetPasswordLength(DependencyObject obj, int value) => obj.SetValue(PasswordLengthProperty, value);
        public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);
        public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);
        public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);
        public static void SetPassword(DependencyObject dp, string value) => dp.SetValue(PasswordProperty, value);
        private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);
        private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetValue(IsUpdatingProperty, value);

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;
            if (!GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
                SetPasswordLength(passwordBox, passwordBox.Password?.Length ?? 0);
                passwordBox.SetSelection(passwordBox.Password?.Length ?? 0, 0);
            }
            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void OnAttachPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }
            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }
            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetPasswordLength(passwordBox, passwordBox.Password?.Length ?? 0);
            SetIsUpdating(passwordBox, false);
        }


        public static void SetSelection(this PasswordBox passwordBox, int start, int length)
            => passwordBox.GetType()
                          .GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                          .Invoke(passwordBox, new object[] { start, length });
    }
}

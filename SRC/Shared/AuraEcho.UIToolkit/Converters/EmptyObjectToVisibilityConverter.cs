using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AuraEcho.UIToolkit.Converters
{
    public class EmptyObjectToVisibilityConverter : DependencyObject, IValueConverter
    {
        /// <summary>
        /// 标识 EmptyValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty EmptyValueProperty =
            DependencyProperty.Register(nameof(EmptyValue), typeof(Visibility), typeof(EmptyObjectToVisibilityConverter), new PropertyMetadata(default(Visibility)));

        /// <summary>
        /// 标识 NotEmptyValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty NotEmptyValueProperty =
            DependencyProperty.Register(nameof(NotEmptyValue), typeof(Visibility), typeof(EmptyObjectToVisibilityConverter), new PropertyMetadata(default(Visibility)));

        public EmptyObjectToVisibilityConverter()
        {
            EmptyValue = Visibility.Collapsed;
            NotEmptyValue = Visibility.Visible;
        }

        /// <summary>
        /// 获取或设置EmptyValue的值
        /// </summary>
        public Visibility EmptyValue
        {
            get => (Visibility)GetValue(EmptyValueProperty);
            set => SetValue(EmptyValueProperty, value);
        }

        /// <summary>
        /// 获取或设置NotEmptyValue的值
        /// </summary>
        public Visibility NotEmptyValue
        {
            get => (Visibility)GetValue(NotEmptyValueProperty);
            set => SetValue(NotEmptyValueProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? EmptyValue : NotEmptyValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

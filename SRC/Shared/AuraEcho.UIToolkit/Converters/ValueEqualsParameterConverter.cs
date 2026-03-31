using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AuraEcho.UIToolkit.Converters
{
    public class ValueEqualsParameterConverter : DependencyObject, IValueConverter
    {
        /// <summary>
        /// 标识 EqualValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty EqualValueProperty =
            DependencyProperty.Register(
                nameof(EqualValue),
                typeof(object),
                typeof(ValueEqualsParameterConverter),
                new PropertyMetadata());

        /// <summary>
        /// 标识 NotEqualValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty NotEqualValueProperty =
            DependencyProperty.Register(
                nameof(NotEqualValue),
                typeof(object),
                typeof(ValueEqualsParameterConverter),
                new PropertyMetadata());

        /// <summary>
        /// 获取或设置 EqualValue 的值
        /// </summary>
        public object EqualValue
        {
            get => GetValue(EqualValueProperty);
            set => SetValue(EqualValueProperty, value);
        }

        /// <summary>
        /// 获取或设置 NotEqualValue 的值
        /// </summary>
        public object NotEqualValue
        {
            get => GetValue(NotEqualValueProperty);
            set => SetValue(NotEqualValueProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => $"{value}" == $"{parameter}"
                ? EqualValue
                : NotEqualValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

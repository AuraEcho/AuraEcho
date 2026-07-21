using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AuraEcho.Toolkit.Wpf.Converters
{
    public class AlternationEqualityConverter : DependencyObject, IMultiValueConverter
    {
        /// <summary>
        /// 标识 EqualValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty OnlyOneValueProperty =
            DependencyProperty.Register(
                nameof(OnlyOneValue),
                typeof(object),
                typeof(AlternationEqualityConverter),
                new PropertyMetadata());

        /// <summary>
        /// 标识 EqualValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty FirstItemValueProperty =
            DependencyProperty.Register(
                nameof(FirstItemValue),
                typeof(object),
                typeof(AlternationEqualityConverter),
                new PropertyMetadata());

        /// <summary>
        /// 标识 NotEqualValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty LastItemValueProperty =
            DependencyProperty.Register(
                nameof(LastItemValue),
                typeof(object),
                typeof(AlternationEqualityConverter),
                new PropertyMetadata());

        /// <summary>
        /// 标识 DefaultValue 依赖属性。
        /// </summary>
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register(
                nameof(DefaultValue),
                typeof(object),
                typeof(AlternationEqualityConverter),
                new PropertyMetadata());

        /// <summary>
        /// 获取或设置 OnlyOneValue 的值
        /// </summary>
        public object OnlyOneValue
        {
            get => GetValue(OnlyOneValueProperty);
            set => SetValue(OnlyOneValueProperty, value);
        }

        /// <summary>
        /// 获取或设置 FirstItemValue 的值
        /// </summary>
        public object FirstItemValue
        {
            get => GetValue(FirstItemValueProperty);
            set => SetValue(FirstItemValueProperty, value);
        }

        /// <summary>
        /// 获取或设置 LastItemValue 的值
        /// </summary>
        public object LastItemValue
        {
            get => GetValue(LastItemValueProperty);
            set => SetValue(LastItemValueProperty, value);
        }

        /// <summary>
        /// 获取或设置 DefaultValue 的值
        /// </summary>
        public object DefaultValue
        {
            get => GetValue(DefaultValueProperty);
            set => SetValue(DefaultValueProperty, value);
        }

        #region Implementation of IMultiValueConverter

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 2 &&
                values[0] is int && values[1] is int)
            {
                if ((int)values[0] == 1)
                {
                    return OnlyOneValue;
                }
                if ((int)values[1] == 0)
                {
                    return FirstItemValue;
                }
                if (Equals((int)values[0], (int)values[1] + 1))
                {
                    return LastItemValue;
                }
            }

            return DefaultValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}

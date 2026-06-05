using System;
using System.Windows;
using System.Windows.Controls;

namespace AuraEcho.Design.AttachedProperties
{
    public static class ControlHelper
    {
        #region CornerRadius

        /// <summary>
        /// 获取控件圆角的半径。
        /// </summary>
        /// <param name="control">要从中读取属性值的元素。</param>
        /// <returns>
        /// 角的圆化程度，表示为 CornerRadius 的值结构。
        /// </returns>
        public static CornerRadius GetCornerRadius(Control control)
        {
            return (CornerRadius)control.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// 设置控件圆角的半径。
        /// </summary>
        /// <param name="control">要设置附加属性的元素。</param>
        /// <param name="value">要设置的属性值。</param>
        public static void SetCornerRadius(Control control, CornerRadius value)
        {
            control.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// 标识 CornerRadius 依赖属性。
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(ControlHelper),
                null);
        #endregion

        #region PlaceHolder

        public static readonly DependencyProperty PlaceHolderProperty =
            DependencyProperty.RegisterAttached(
                "PlaceHolder",
                typeof(string),
                typeof(ControlHelper),
                new PropertyMetadata(String.Empty));

        public static string GetPlaceHolder(Control control)
        {
            return (string)control.GetValue(PlaceHolderProperty);
        }

        public static void SetPlaceHolder(Control control, string value)
        {
            control.SetValue(PlaceHolderProperty, value);
        }
        #endregion
    }
}

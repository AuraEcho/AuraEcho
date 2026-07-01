using System;
using System.Windows;

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
        public static CornerRadius GetCornerRadius(DependencyObject obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// 设置控件圆角的半径。
        /// </summary>
        /// <param name="obj">要设置附加属性的元素。</param>
        /// <param name="value">要设置的属性值。</param>
        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
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

        public static string GetPlaceHolder(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceHolderProperty);
        }

        public static void SetPlaceHolder(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceHolderProperty, value);
        }
        #endregion

        #region IsBusy

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached(
                "IsBusy",
                typeof(bool),
                typeof(ControlHelper),
                new PropertyMetadata(false));

        public static bool GetIsBusy(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsBusyProperty);
        }

        public static void SetIsBusy(DependencyObject obj, bool value)
        {
            obj.SetValue(IsBusyProperty, value);
        }
        #endregion

        #region BusyContent

        public static readonly DependencyProperty BusyContentProperty =
            DependencyProperty.RegisterAttached(
                "BusyContent",
                typeof(string),
                typeof(ControlHelper),
                new PropertyMetadata(string.Empty));

        public static string GetBusyContent(DependencyObject obj)
        {
            return (string)obj.GetValue(BusyContentProperty);
        }

        public static void SetBusyContent(DependencyObject obj, string value)
        {
            obj.SetValue(BusyContentProperty, value);
        }
        #endregion
    }
}

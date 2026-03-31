using System.Windows;
using System.Windows.Media;

namespace AuraEcho.UIToolkit.Tools
{
    public static class VisualTreeUtil
    {
        public static ChildType FindVisualChild<ChildType>(DependencyObject obj) where ChildType : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildType)
                    return (ChildType)child;
                else
                {
                    ChildType childOfChild = FindVisualChild<ChildType>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}
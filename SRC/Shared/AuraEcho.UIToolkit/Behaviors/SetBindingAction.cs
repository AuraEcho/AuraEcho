using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.UIToolkit.Behaviors
{
    public class SetBindingAction : TargetedTriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty PropertyNameProperty
            = DependencyProperty.Register(
                "PropertyName",
                typeof(string),
                typeof(SetBindingAction),
                null);

        public SetBindingAction()
        {
        }

        /// <summary>
        /// 目标属性名称
        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        /// <summary>
        /// 目标属性应用的绑定
        /// </summary>
        public Binding Binding { get; set; }

        protected override void Invoke(object parameter)
        {
            if (AssociatedObject is null) return;

            var target = ResolveTarget();
            if (target is null) return;

            if (string.IsNullOrEmpty(PropertyName)) return;

            if (Binding is null)
            {
                var dp = ResolveDependencyProperty(target, PropertyName);
                if (dp != null)
                    BindingOperations.ClearBinding(target, dp);
                return;
            }

            var targetDp = ResolveDependencyProperty(target, PropertyName);
            if (targetDp is null) return;

            BindingOperations.SetBinding(target, targetDp, Binding);
        }

        /// <summary>
        /// 解析目标元素
        /// </summary>
        private FrameworkElement ResolveTarget()
        {
            if (Target != null) return Target;

            if (string.IsNullOrEmpty(TargetName)) return null;

            if (AssociatedObject is FrameworkElement fe)
                return fe.FindName(TargetName) as FrameworkElement;

            return null;
        }

        private static DependencyProperty ResolveDependencyProperty(DependencyObject target, string propertyName)
        {
            var dpFieldName = propertyName + "Property";
            var targetType = target.GetType();

            var field = targetType.GetField(
                dpFieldName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return field?.GetValue(null) as DependencyProperty;
        }
    }
}

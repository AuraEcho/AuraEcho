using System.Reflection;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.UIToolkit.Behaviors
{
    public class ReferenceResourceAction : TargetedTriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty PropertyNameProperty
            = DependencyProperty.Register(
                "TargetProperty",
                typeof(string),
                typeof(ReferenceResourceAction),
                null);

        public static readonly DependencyProperty ResourceKeyProperty
            = DependencyProperty.Register(
                "ResourceKey",
                typeof(string),
                typeof(ReferenceResourceAction),
                null);

        public ReferenceResourceAction()
        {
        }

        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        public string ResourceKey
        {
            get => (string)GetValue(ResourceKeyProperty);
            set => SetValue(ResourceKeyProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if (AssociatedObject is null) return;

            if (PropertyName is null) return;

            var target = ResolveTarget();
            if (target is null) return;

            DependencyProperty targetDp = ResolveDependencyProperty(target, PropertyName);

            target.SetResourceReference(targetDp, ResourceKey);
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

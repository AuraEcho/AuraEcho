using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

namespace AuraEcho.Behaviors;

public class I18NBindingAction : TargetedTriggerAction<FrameworkElement>
{
    public static readonly DependencyProperty I18NTokenNameProperty
        = DependencyProperty.Register(
            "I18NTokenName",
            typeof(string),
            typeof(I18NBindingAction),
            null);

    public static readonly DependencyProperty PropertyNameProperty
        = DependencyProperty.Register(
            "PropertyName",
            typeof(string),
            typeof(I18NBindingAction),
            null);

    public I18NBindingAction()
    {
    }

    public string I18NTokenName
    {
        get => (string)GetValue(I18NTokenNameProperty);
        set => SetValue(I18NTokenNameProperty, value);
    }

    /// <summary>
    /// 目标属性名称
    public string PropertyName
    {
        get => (string)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        if (AssociatedObject is null) return;

        var target = ResolveTarget();
        if (target is null) return;

        if (String.IsNullOrEmpty(I18NTokenName)) return;

        if (String.IsNullOrEmpty(PropertyName)) return;

        var targetDp = ResolveDependencyProperty(target, PropertyName);
        if (targetDp is null) return;

        Binding i18NBinding = new Binding
        {
            Source = Application.Current.FindResource("Loc"),
            Path = new PropertyPath(I18NTokenName),
            Mode = BindingMode.OneWay
        };

        BindingOperations.SetBinding(target, targetDp, i18NBinding);
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

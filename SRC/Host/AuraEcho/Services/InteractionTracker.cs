using AuraEcho.Telemetry;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.Services;

/// <summary>
/// 全局 UI 交互自动捕获器。
/// 通过 <see cref="EventManager.RegisterClassHandler(System.Type, RoutedEvent, System.Delegate)"/>
/// 在应用级别监听按钮点击、菜单点击、开关切换与选择变更等路由事件，
/// 自动上报为 <c>UI.*</c> 遥测事件，无需在每个控件上单独埋点。
/// 事件的所在页面由遥测环境上下文自动盖章；此处只负责提取控件标识。
/// </summary>
/// <remarks>
/// 出于隐私考虑，不捕获文本输入框的内容。控件文本仅在无名称/自动化标识时作为兜底，
/// 且做长度截断。原生 WPF 插件的控件位于同一可视化树，其交互也会被此处自动覆盖。
/// </remarks>
public sealed class InteractionTracker
{
    // 控件兜底文本的最大长度
    private const int MAX_LABEL_LENGTH = 64;

    private readonly ITelemetryService _telemetry;
    private bool _registered;

    public InteractionTracker(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    /// <summary>
    /// 注册全局路由事件处理器。应在应用启动、主窗口创建后调用一次。
    /// </summary>
    public void Register()
    {
        if (_registered) return;
        _registered = true;

        EventManager.RegisterClassHandler(typeof(ButtonBase),
            ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick), handledEventsToo: true);

        EventManager.RegisterClassHandler(typeof(MenuItem),
            MenuItem.ClickEvent, new RoutedEventHandler(OnMenuClick), handledEventsToo: true);

        EventManager.RegisterClassHandler(typeof(ToggleButton),
            ToggleButton.CheckedEvent, new RoutedEventHandler(OnToggle), handledEventsToo: true);
        EventManager.RegisterClassHandler(typeof(ToggleButton),
            ToggleButton.UncheckedEvent, new RoutedEventHandler(OnToggle), handledEventsToo: true);

        EventManager.RegisterClassHandler(typeof(Selector),
            Selector.SelectionChangedEvent, new SelectionChangedEventHandler(OnSelectionChanged), handledEventsToo: true);
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        _telemetry.TrackEvent("UI.Click", BuildProperties(element));
    }

    private void OnMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        _telemetry.TrackEvent("UI.MenuClick", BuildProperties(element));
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle) return;
        var props = BuildProperties(toggle);
        props["checked"] = toggle.IsChecked == true ? "true" : "false";
        _telemetry.TrackEvent("UI.Toggle", props);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        // 不记录选中项内容（可能含 PII）
        _telemetry.TrackEvent("UI.SelectionChanged", BuildProperties(element));
    }

    /// <summary>
    /// 提取控件标识
    /// </summary>
    private static Dictionary<string, string> BuildProperties(FrameworkElement element)
    {
        var props = new Dictionary<string, string>
        {
            ["controlType"] = element.GetType().Name
        };

        var id = ResolveElementId(element);
        if (!string.IsNullOrWhiteSpace(id))
            props["element"] = id;

        return props;
    }

    private static string ResolveElementId(FrameworkElement element)
    {
        var automationName = AutomationProperties.GetName(element);
        if (!string.IsNullOrWhiteSpace(automationName))
            return Truncate(automationName);

        if (!string.IsNullOrWhiteSpace(element.Name))
            return element.Name;

        // 兜底：使用按钮类的文本内容
        if (element is ContentControl { Content: string text } && !string.IsNullOrWhiteSpace(text))
            return Truncate(text);

        return string.Empty;
    }

    private static string Truncate(string value)
        => value.Length <= MAX_LABEL_LENGTH ? value.Trim() : value[..MAX_LABEL_LENGTH].Trim();
}

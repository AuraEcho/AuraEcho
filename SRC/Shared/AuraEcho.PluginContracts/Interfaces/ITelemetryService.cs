using System;
using System.Collections.Generic;

namespace AuraEcho.PluginContracts.Interfaces
{
    /// <summary>
    /// 遥测服务接口 —— 用于采集应用使用数据、异常报告和性能指标。
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// 是否启用遥测数据采集
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 记录自定义事件。
        /// </summary>
        /// <param name="name">事件名称（如 "App.Launch", "Plugin.Installed"）</param>
        /// <param name="properties">附加字符串属性（可选）</param>
        void TrackEvent(string name, IDictionary<string, string> properties = null);

        /// <summary>
        /// 记录数值指标。
        /// </summary>
        /// <param name="name">指标名称（如 "App.StartupTime", "Memory.WorkingSet"）</param>
        /// <param name="value">指标数值</param>
        /// <param name="properties">附加字符串属性（可选）</param>
        void TrackMetric(string name, double value, IDictionary<string, string> properties = null);

        /// <summary>
        /// 记录异常事件。
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="properties">附加字符串属性（可选，如 {"source": "Dispatcher"}）</param>
        void TrackException(Exception exception, IDictionary<string, string> properties = null);

        /// <summary>
        /// 记录页面事件。
        /// </summary>
        /// <param name="pageName">页面名称（如 "Homepage", "Settings"）</param>
        void TrackPageView(string pageName);
    }
}

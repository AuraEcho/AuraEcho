using System;
using System.Collections.Generic;

namespace AuraEcho.Telemetry
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
        /// <param name="name">事件名称</param>
        /// <param name="properties">附加字符串属性</param>
        void TrackEvent(string name, Dictionary<string, string> properties = null);

        /// <summary>
        /// 记录数值指标。
        /// </summary>
        /// <param name="name">指标名称</param>
        /// <param name="metrics">指标数值</param>
        /// <param name="properties">附加字符串属性</param>
        void TrackMetric(string name, Dictionary<string, double> metrics, Dictionary<string, string> properties = null);

        /// <summary>
        /// 记录异常事件。
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="properties">附加字符串属性</param>
        void TrackException(Exception exception, Dictionary<string, string> properties = null);

        /// <summary>
        /// 记录页面事件。
        /// </summary>
        /// <param name="pageName">页面名称</param>
        /// <param name="properties">附加字符串属性</param>
        void TrackPageView(string pageName, Dictionary<string, string> properties = null);
    }
}

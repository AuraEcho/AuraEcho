using AuraEcho.PluginContracts.Interfaces;
using System;
using System.Collections.Generic;

namespace AuraEcho.PluginContracts.Telemetry
{
    /// <summary>
    /// 面向插件的遥测包装器。
    /// </remarks>
    public sealed class PluginTelemetryService : ITelemetryService
    {
        private const string NAME_PREFIX = "Plugin.";

        private readonly ITelemetryService _inner;
        private readonly string _pluginId;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inner">从宿主容器解析出的遥测服务。</param>
        /// <param name="pluginId">当前插件的唯一标识，将写入每条事件的 <c>pluginId</c> 属性。</param>
        public PluginTelemetryService(ITelemetryService inner, string pluginId)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _pluginId = pluginId ?? string.Empty;
        }

        /// <inheritdoc />
        public bool IsEnabled
        {
            get => _inner.IsEnabled;
            set => _inner.IsEnabled = value;
        }

        /// <inheritdoc />
        public void TrackEvent(string name, Dictionary<string, string> properties = null)
            => _inner.TrackEvent(Prefix(name), Tag(properties));

        /// <inheritdoc />
        public void TrackMetric(string name, Dictionary<string, double> metrics, Dictionary<string, string> properties = null)
            => _inner.TrackMetric(Prefix(name), metrics, Tag(properties));

        /// <inheritdoc />
        public void TrackException(Exception exception, Dictionary<string, string> properties = null)
            => _inner.TrackException(exception, Tag(properties));

        /// <inheritdoc />
        public void TrackPageView(string pageName, Dictionary<string, string> properties = null)
            => _inner.TrackPageView(Prefix(pageName), Tag(properties));

        private string Prefix(string name)
            => string.IsNullOrEmpty(name) ? name : NAME_PREFIX + name;

        /// <summary>
        /// 为属性字典注入来源插件标识
        /// </summary>
        private Dictionary<string, string> Tag(Dictionary<string, string> properties)
        {
            var tagged = properties is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(properties);
            tagged["pluginId"] = _pluginId;
            return tagged;
        }
    }
}

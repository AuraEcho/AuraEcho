using System;

namespace AuraEcho.PluginContracts.Attributes
{
    /// <summary>
    /// 模块默认视图
    /// </summary>
    /// <param name="viewName"></param>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class PluginDefaultViewAttribute : Attribute
    {
        public PluginDefaultViewAttribute(string viewName)
        {
            ViewName = viewName;
        }
        public string ViewName { get; }
    }
}

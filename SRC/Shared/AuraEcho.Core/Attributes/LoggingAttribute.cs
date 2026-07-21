using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rougamo;
using Rougamo.Context;

namespace AuraEcho.Core.Attributes;

/// <summary>
/// 标记一个方法，使其在执行前后或抛出异常时，打印日志。
/// </summary>
public class LoggingAttribute : MoAttribute
{
    /// <summary>
    /// 日志工厂。
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; set; }

    private static ILogger GetLogger(MethodContext context)
    {
        var declaringType = context.Method.DeclaringType;
        if (LoggerFactory is null || declaringType is null)
            return NullLogger.Instance;

        return LoggerFactory.CreateLogger(declaringType);
    }

    /// <summary>
    /// 方法执行前。
    /// </summary>
    /// <param name="context"></param>
    public override void OnEntry(MethodContext context)
    {
        var logger = GetLogger(context);
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        string parameters =
            string.Join(", ", context.Method.GetParameters().Select(p => $"{p.ParameterType} {p.Name}"));

        logger.LogDebug("Entry: {DeclaringType}.{Method}({Parameters})",
            context.Method.DeclaringType?.Name, context.Method.Name, parameters);
    }

    /// <summary>
    /// 方法执行异常。
    /// </summary>
    /// <param name="context"></param>
    public override void OnException(MethodContext context)
    {
        var logger = GetLogger(context);
        logger.LogError(context.Exception, "方法执行异常: {DeclaringType}.{Method}",
            context.Method.DeclaringType?.Name, context.Method.Name);
    }

    /// <summary>
    /// 方法退出时。
    /// </summary>
    /// <param name="context"></param>
    public override void OnExit(MethodContext context)
    {
        base.OnExit(context);
    }

    /// <summary>
    /// 方法执行成功后。
    /// </summary>
    /// <param name="context"></param>
    public override void OnSuccess(MethodContext context)
    {
        base.OnSuccess(context);
    }
}

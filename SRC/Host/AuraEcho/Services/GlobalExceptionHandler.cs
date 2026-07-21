using AuraEcho.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AuraEcho.PluginContracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuraEcho.Services;

/// <summary>
/// 全局未处理异常捕获器。
/// </summary>
public sealed class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ITelemetryService _telemetry;
    private bool _registered;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, ITelemetryService telemetry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry;
    }

    /// <summary>
    /// 订阅全局异常处理事件。
    /// </summary>
    public void Register()
    {
        if (_registered) return;
        _registered = true;

        // Task 线程内未捕获异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // UI 主线程未捕获异常
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 非 UI 线程未捕获异常（例如自行创建的子线程）
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception, "TaskScheduler");
        }
        catch (Exception ex)
        {
            HandleException(ex, "TaskScheduler");
        }
        finally
        {
            e.SetObserved();
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception, "Dispatcher");
        }
        catch (Exception ex)
        {
            HandleException(ex, "Dispatcher");
        }
        finally
        {
            e.Handled = true;
        }
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
                HandleException(exception, "AppDomain");
        }
        catch (Exception ex)
        {
            HandleException(ex, "AppDomain");
        }
    }

    /// <summary>
    /// 统一的异常处理逻辑：记录日志并上报遥测。
    /// </summary>
    private void HandleException(Exception exception, string source = "Unknown")
    {
        _logger.LogCritical(exception, "未处理的应用程序异常，来源: {Source}", source);

        try
        {
            _telemetry?.TrackException(exception, new Dictionary<string, string>
            {
                ["source"] = source
            });
        }
        catch
        {
            // 遥测上报失败不应影响异常处理主流程
        }
    }
}

using AuraEcho.Core.Tools;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AuraEcho.Tools;

public static class WebViewEnvironment
{
    public static CoreWebView2Environment Default { get; set; }

    public static async Task InitAllEnvironmentsAsync()
    {
        await InitDefaultEnvironmentAsync();
    }

    private static async Task InitDefaultEnvironmentAsync()
    {
        if (Default is not null) return;

        var userDataFolder = Path.Combine(ApplicationPaths.WebViewCacheRoot, "Default");
        Directory.CreateDirectory(userDataFolder);

        var args = new List<string>
        {
            // 强制启用硬件加速
            "--ignore-gpu-blocklist", 
            
            // 强制 GPU 栅格化提高渲染性能
            "--enable-gpu-rasterization",  
            
            // 禁用 Edge 自带的反钓鱼检测以加快加载
            "--disable-features=EdgeAntiPhishing" 
        };

        // 3. 配置 Options
        var options = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = String.Join(" ", args),
            Language = "zh-CN",
            
            // 允许使用 Windows 系统的账号自动登录支持的微软网站
            AllowSingleSignOnUsingOSPrimaryAccount = true 
        };

        Default = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
    }
}

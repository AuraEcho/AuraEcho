using System.Runtime.InteropServices;

namespace AuraEcho.Toolkit.Native;

public static class Win32Helper
{
    /// <summary>
    /// 查找窗口句柄
    /// </summary>
    /// <param name="lpClassName">ClassName</param>
    /// <param name="lpWindowName">WindowName</param>
    /// <returns>窗口句柄</returns>
    [DllImport("USER32.DLL", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// <summary>
    /// 通过窗口句柄激活窗口
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns></returns>
    [DllImport("USER32.DLL")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}

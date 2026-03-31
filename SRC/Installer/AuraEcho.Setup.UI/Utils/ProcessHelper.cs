using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AuraEcho.Setup.UI.Utils
{
    public static class ProcessHelper
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public static string GetExecutablePath(this Process process)
        {
            int capacity = 1024;
            StringBuilder sb = new StringBuilder(capacity);
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);

            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                    {
                        return sb.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            return null;
        }
    }
}
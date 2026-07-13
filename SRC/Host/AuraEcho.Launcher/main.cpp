#include <windows.h>
#include <gdiplus.h>
#include <string>
#include <dwmapi.h>
#include <shlobj.h>
#include <vector>
#include <thread>
#include <filesystem>
#include <fstream>
#include <chrono>
#include <iomanip>
#include <tlhelp32.h>

#include "resource.h"
#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "dwmapi.lib")

using namespace Gdiplus;
namespace fs = std::filesystem;

LPCTSTR LAUNCHER_SERVICE_PIPE_NAME = L"\\\\.\\pipe\\AURAECHO_LAUNCHER_SERVICE_PIPE";
LPCTSTR AURAECHO_PIPE_NAME = L"\\\\.\\pipe\\AURAECHO_APP_PIPE";
LPCTSTR INSTALLER_PIPE_NAME = L"\\\\.\\pipe\\AuraEcho_Installer_Pipe";

LPCTSTR AURAECHO_MUTEX_ID = TEXT("E2A4C483-C59D-4856-BE14-F9B4AF07042C");
LPCTSTR INSTALLER_MUTEX_ID = TEXT("Global\\17FA29D6-F4BC-4720-A55C-27042D247E35");

const std::string APP_SHOW = "ShowWindow";
const std::string LAUNCH_APP_WHEN_INSTALLED = "LaunchAppWhenInstalled";

void WriteLog(const std::string& text) {

    PWSTR path_tmp;
    if (SHGetKnownFolderPath(FOLDERID_ProgramData, 0, NULL, &path_tmp) == S_OK) {

        std::filesystem::path logPath(path_tmp);
        CoTaskMemFree(path_tmp); // 释放内存

        logPath /= "AuraEcho\\Client\\Logs";

        // 确保文件夹存在
        std::error_code ec;
        std::filesystem::create_directories(logPath, ec);

        logPath /= "launcher.log";
        std::ofstream logFile(logPath, std::ios_base::out | std::ios_base::app);

        auto now = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());
        struct tm timeInfo;

        if (localtime_s(&timeInfo, &now) == 0) {
            logFile << std::put_time(&timeInfo, "%Y-%m-%d %H:%M:%S") << " - " << text << std::endl;
        }
    }
}

Image* LoadImageFromResource(HMODULE hMod, int resId, LPCWSTR resType) {
    HRSRC hRes = FindResource(hMod, MAKEINTRESOURCE(resId), resType);
    if (!hRes) return nullptr;

    DWORD resSize = SizeofResource(hMod, hRes);
    HGLOBAL hResData = LoadResource(hMod, hRes);
    if (!hResData) return nullptr;

    void* pRes = LockResource(hResData);

    // 将资源数据拷贝到流中
    IStream* pStream = nullptr;
    HGLOBAL hMem = GlobalAlloc(GMEM_MOVEABLE, resSize);
    if (hMem) {
        void* pLockedMem = GlobalLock(hMem);
        memcpy(pLockedMem, pRes, resSize);
        GlobalUnlock(hMem);
        CreateStreamOnHGlobal(hMem, TRUE, &pStream);
    }

    if (pStream) {
        Image* pImage = Image::FromStream(pStream);
        pStream->Release();
        return pImage;
    }
    return nullptr;
}

std::string GetAppInstallPath() {
    std::string result = "";
    HKEY hKey;
    const wchar_t* subKey = L"SOFTWARE\\AuraEcho";
    const wchar_t* valueName = L"AppPath";

    LSTATUS status = RegOpenKeyExW(HKEY_LOCAL_MACHINE, subKey, 0, KEY_READ, &hKey);

    if (status == ERROR_SUCCESS) {
        DWORD bufferSize = 0;

        // 获取数据缓冲区大小
        status = RegQueryValueExW(hKey, valueName, NULL, NULL, NULL, &bufferSize);

        if (status == ERROR_SUCCESS && bufferSize > 0) {
            std::vector<wchar_t> buffer(bufferSize / sizeof(wchar_t));

            // 获取实际数据
            status = RegQueryValueExW(hKey, valueName, NULL, NULL, (LPBYTE)buffer.data(), &bufferSize);

            if (status == ERROR_SUCCESS) {
                std::wstring wPath(buffer.data());

                // 将 wstring 转换为 string (假设路径是 ANSI/UTF-8 编码)
                int size_needed = WideCharToMultiByte(CP_UTF8, 0, wPath.c_str(), (int)wPath.length(), NULL, 0, NULL, NULL);
                result.resize(size_needed);
                WideCharToMultiByte(CP_UTF8, 0, wPath.c_str(), (int)wPath.length(), &result[0], size_needed, NULL, NULL);
            }
        }
        RegCloseKey(hKey);
    }

    return result;
}

static bool SendPipeMessage(LPCTSTR pipeName, const std::string& message) {
    HANDLE hPipe = CreateFile(
        pipeName,
        GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        0,
        NULL);

    if (hPipe != INVALID_HANDLE_VALUE) {
        DWORD bytesWritten;
        WriteFile(hPipe, message.c_str(), (DWORD)message.length(), &bytesWritten, NULL);
        CloseHandle(hPipe);
        return true;
    }
    else {
        OutputDebugString(L"无法连接到命名管道服务端\n");
        return false;
    }
}
static void EnableWin11RoundedCorner(HWND hwnd)
{
    DWORD preference = DWMWCP_ROUND;
    DwmSetWindowAttribute(
        hwnd,
        DWMWA_WINDOW_CORNER_PREFERENCE,
        &preference,
        sizeof(preference)
    );
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    static Image* pImage = nullptr;
    switch (uMsg) {
    case WM_CREATE: {
        pImage = LoadImageFromResource(GetModuleHandle(nullptr), IDB_PNG1, L"PNG");
        EnableWin11RoundedCorner(hwnd);
        return 0;
    }

    case WM_NCHITTEST: {
        return HTCAPTION;
    }

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, &ps);
        Graphics graphics(hdc);

        if (pImage && pImage->GetLastStatus() == Ok) {
            RECT rc;
            GetClientRect(hwnd, &rc);
            graphics.DrawImage(pImage, 0, 0, rc.right, rc.bottom);
        }
        else {
            SolidBrush brush(Color(255, 200, 200, 200));
            graphics.FillRectangle(&brush, 0, 0, 512, 315);
        }

        EndPaint(hwnd, &ps);
        return 0;
    }
    case WM_DESTROY:
        delete pImage;
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

void CenterWindow(HWND hwnd) {
    RECT rc;
    GetWindowRect(hwnd, &rc);
    int xPos = (GetSystemMetrics(SM_CXSCREEN) - (rc.right - rc.left)) / 2;
    int yPos = (GetSystemMetrics(SM_CYSCREEN) - (rc.bottom - rc.top)) / 2;
    SetWindowPos(hwnd, 0, xPos, yPos, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
}

// 根据路径获取进程 PID
static DWORD GetPidByPath(const std::string& targetPath) {
    DWORD pid = 0;
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) return 0;

    PROCESSENTRY32W pe;
    pe.dwSize = sizeof(pe);

    if (Process32FirstW(hSnapshot, &pe)) {
        do {
            // 获取进程的全路径进行比对
            HANDLE hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pe.th32ProcessID);
            if (hProcess) {
                wchar_t path[MAX_PATH];
                DWORD size = MAX_PATH;
                if (QueryFullProcessImageNameW(hProcess, 0, path, &size)) {
                    std::wstring wTargetPath(targetPath.begin(), targetPath.end());
                    if (_wcsicmp(path, wTargetPath.c_str()) == 0) {
                        pid = pe.th32ProcessID;
                    }
                }
                CloseHandle(hProcess);
            }
            if (pid != 0) break;
        } while (Process32NextW(hSnapshot, &pe));
    }
    CloseHandle(hSnapshot);
    return pid;
}

// 查找属于特定 PID 的可见主窗口
struct EnumData { DWORD pid; HWND hwnd; };
BOOL CALLBACK EnumProc(HWND hwnd, LPARAM lParam) {
    EnumData* data = (EnumData*)lParam;
    DWORD windowPid;
    GetWindowThreadProcessId(hwnd, &windowPid);
    if (windowPid == data->pid && IsWindowVisible(hwnd) && GetParent(hwnd) == NULL) {
        data->hwnd = hwnd;
        return FALSE;
    }
    return TRUE;
}
static void StartApp(HWND hwndTarget) {
    std::string appPath = GetAppInstallPath();
    bool sendResult = SendPipeMessage(LAUNCHER_SERVICE_PIPE_NAME, appPath);

    if (!sendResult) {
        PostMessage(hwndTarget, WM_CLOSE, 0, 0);
        return;
    }

    // 等待启动并获取启动路径对应的 PID
    DWORD targetPid = 0;
    for (int i = 0; i < 100 && targetPid == 0; i++) {
        targetPid = GetPidByPath(appPath);
        if (targetPid == 0) Sleep(50);
    }

    if (targetPid == 0) {
        WriteLog("Error：未找到进程");
        PostMessage(hwndTarget, WM_CLOSE, 0, 0);
        return;
    }

    // 等待主窗口显示
    EnumData ed = { targetPid, NULL };
    for (int i = 0; i < 100; i++) {
        EnumWindows(EnumProc, (LPARAM)&ed);
        if (ed.hwnd) {
            WriteLog("已激活 App 主窗口");
            SetForegroundWindow(ed.hwnd);
            SetFocus(ed.hwnd);
            break;
        }
        Sleep(50);
    }

    PostMessage(hwndTarget, WM_CLOSE, 0, 0);
}

static bool CheckMutex(LPCWSTR mutexId, DWORD timeoutMs) {

    HANDLE hAppMutex = CreateMutex(NULL, FALSE, mutexId);

    if (hAppMutex == NULL) {
        return true;
    }

    // WAIT_OBJECT_0: 成功获取到了互斥体
    // WAIT_TIMEOUT: 在指定时间内没等到
    DWORD result = WaitForSingleObject(hAppMutex, timeoutMs);

    if (result == WAIT_OBJECT_0 || result == WAIT_ABANDONED) {
        ReleaseMutex(hAppMutex);
        CloseHandle(hAppMutex);
        return false;
    }

    // 超时或其他错误 (WAIT_TIMEOUT, WAIT_FAILED)
    CloseHandle(hAppMutex);
    return true;
}

static bool AppIsRunning() {
    return CheckMutex(AURAECHO_MUTEX_ID, 0);
}

static bool AppInstallerIsRunning() {
    return CheckMutex(INSTALLER_MUTEX_ID, 10000);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR pCmdLine, int nCmdShow) {
    WriteLog("程序已启动");

    std::string cmdLine(pCmdLine);
    bool isHideRunning = cmdLine.find("-hide") != std::string::npos;

    if (AppIsRunning()) {
        WriteLog("正在退出：App 正在运行");
        SendPipeMessage(AURAECHO_PIPE_NAME, APP_SHOW);
        return 0;
    }

    if (AppInstallerIsRunning()) {
        if (isHideRunning) {
            SendPipeMessage(INSTALLER_PIPE_NAME, LAUNCH_APP_WHEN_INSTALLED);
            WriteLog("正在退出：安装程序正在运行，已向安装程序发送启动信号");
            return 0;
        }

        WriteLog("正在退出：安装程序正在运行");
        return 0;
    }

    if (isHideRunning) {
        std::string message = GetAppInstallPath() + " -hide";
        SendPipeMessage(LAUNCHER_SERVICE_PIPE_NAME, message);
        return 0;
    }

    GdiplusStartupInput gdiplusStartupInput;
    ULONG_PTR gdiplusToken;
    GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

    const wchar_t CLASS_NAME[] = L"AuraEchoLauncherMainWindowClass";
    WNDCLASS wc = { };
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);

    RegisterClass(&wc);

    HWND hwnd = CreateWindowEx(
        WS_EX_TOOLWINDOW,
        CLASS_NAME,
        L"AuraEchoLauncher",
        WS_POPUP,
        CW_USEDEFAULT, CW_USEDEFAULT, 512, 315,
        NULL, NULL, hInstance, NULL
    );

    CenterWindow(hwnd);
    ShowWindow(hwnd, nCmdShow);

    SetForegroundWindow(hwnd);
    SetFocus(hwnd);

    std::thread(StartApp, hwnd).detach();

    MSG msg = { };
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    GdiplusShutdown(gdiplusToken);
    return 0;
}
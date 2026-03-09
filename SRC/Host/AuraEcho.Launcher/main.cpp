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

#include "resource.h"
#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "dwmapi.lib")

using namespace Gdiplus;
namespace fs = std::filesystem;

LPCTSTR LAUNCHER_SERVICE_PIPE_NAME = L"\\\\.\\pipe\\AURAECHO_LAUNCHER_SERVICE_PIPE";
LPCTSTR AURAECHO_PIPE_NAME = L"\\\\.\\pipe\\AuraEcho_SingleInstance_Pipe";
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

        logPath /= "AuraEcho\\Launcher";

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


static void StartApp(HWND hwndTarget) {
    LPCWSTR TARGET_WINDOW_TITLE = L"AuraEcho";

    std::string message = GetAppInstallPath();
    bool sendResult = SendPipeMessage(LAUNCHER_SERVICE_PIPE_NAME, message);

    if (!sendResult) {
        PostMessage(hwndTarget, WM_CLOSE, 0, 0);
        return;
    }

    int attempts = 0;
    while (attempts < 100) {
        HWND targetHwnd = FindWindow(NULL, TARGET_WINDOW_TITLE);

        if (targetHwnd != NULL && IsWindowVisible(targetHwnd)) {
            OutputDebugString(L"Target window found! Closing launcher...\n");

            // 激活窗口(防止主程序窗口不在最前)
            SetForegroundWindow(targetHwnd);
            SetFocus(targetHwnd);

            PostMessage(hwndTarget, WM_CLOSE, 0, 0);
            return;
        }

        Sleep(200);
        attempts++;
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
    WriteLog("--- Application Starting ---");

    std::string cmdLine(pCmdLine);
    bool isHideRunning = cmdLine.find("-hide") != std::string::npos;

    if (AppIsRunning()) {
        WriteLog("Info: App is already running. Sending APP_SHOW to pipe and exiting.");
        SendPipeMessage(AURAECHO_PIPE_NAME, APP_SHOW);
        return 0;
    }

    if (AppInstallerIsRunning()) {
        if (isHideRunning) {
            WriteLog("Info: Installer is running and '-hide' flag detected. Sending LaunchAppWhenInstalled to pipe.");
            SendPipeMessage(INSTALLER_PIPE_NAME, LAUNCH_APP_WHEN_INSTALLED);
            return 0;
        }

        WriteLog("Warning: Installer is currently running. Exiting process.");
        return 0;
    }

    WriteLog("CmdLine received: " + cmdLine);

    if (isHideRunning) {
        std::string message = GetAppInstallPath() += " -hide";
        WriteLog("Info: Found '-hide' flag. Sending message to Launcher Service: " + message);
        SendPipeMessage(LAUNCHER_SERVICE_PIPE_NAME, message);
        return 0;
    }

    WriteLog("Success: Proceeding to main application initialization.");

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

    std::thread(StartApp, hwnd).detach();

    MSG msg = { };
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    GdiplusShutdown(gdiplusToken);
    return 0;
}
//
// Main.cpp in SetupAppLauncher.exe
//
// Launches the app or document that is passed to it on the command line
//
#include <windows.h>
#include <strsafe.h>

//////////////////////////////////////////////////////////////////////////////
// LaunchTray
int APIENTRY wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
    {
    BOOL fSuccess = true;

    // The app to launch is just the entire command line
    LPWSTR path = lpCmdLine;

    const int cchMax = 128; WCHAR buffer[cchMax];
    StringCchPrintf(buffer, cchMax, L"BotBug: SetupAppLauncher: starting(%s)", path); OutputDebugString(buffer);

    SHELLEXECUTEINFO sei;
    ZeroMemory(&sei, sizeof(SHELLEXECUTEINFO));
    sei.fMask = SEE_MASK_FLAG_NO_UI; // don't show error UI, we'll just silently fail
    sei.hwnd = nullptr;
    sei.lpVerb = nullptr;
    sei.lpFile = path;
    sei.lpParameters = nullptr;
    sei.lpDirectory = nullptr;
    sei.nShow = SW_SHOWNORMAL;
    sei.cbSize = sizeof(sei);

    // Launch the program
    fSuccess = ShellExecuteEx(&sei);

    // Note: we always say we succeed, as it's not worth bothering the installer 
    // with an error for a simple run-app launcher such as this.
    OutputDebugString(L"BotBug: SetupAppLauncher: ...exiting");
    return ERROR_SUCCESS;
    }
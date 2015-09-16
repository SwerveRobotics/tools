//
// CustomAction.cpp in SwerveToolsSetupTrayLauncher.dll
//
#include <windows.h>
#include "msiquery.h"

//////////////////////////////////////////////////////////////////////////////
// LaunchTray
//
// Launches SwerveToolsTray.exe at the end of a successful setup
//
extern "C" UINT __stdcall LaunchTray(MSIHANDLE hInstall)
    {
    BOOL fSuccess = false;
    OutputDebugString(L"BotBug: LaunchTray: starting...");

    LPCWSTR path = L"c:\\program files (x86)\\Swerve Robotics\\Tools Suite\\SwerveToolsTray.exe";

    if (false)
        {
        SHELLEXECUTEINFO sei;
        ZeroMemory(&sei, sizeof(SHELLEXECUTEINFO));
        sei.fMask        = SEE_MASK_FLAG_NO_UI; // don't show error UI, we'll just silently fail
        sei.hwnd         = nullptr;
        sei.lpVerb       = nullptr;
        sei.lpFile       = path;
        sei.lpParameters = nullptr;
        sei.lpDirectory  = nullptr;
        sei.nShow        = SW_SHOWNORMAL;
        sei.cbSize       = sizeof(sei);

        // Launch the program
        fSuccess = ShellExecuteEx(&sei);
        }

    OutputDebugString(L"BotBug: LaunchTray: ...exiting!");

    // Note: we always say we succeed, as it's not worth bothering the installer 
    // with an error for a simple run-app launcher such as this.
    return ERROR_SUCCESS;
    }
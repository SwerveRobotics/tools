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


    OutputDebugString(L"BotBug: LaunchTray: ...exiting");

    return (fSuccess) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    }
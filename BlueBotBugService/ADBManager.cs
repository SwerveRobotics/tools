using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

// https://madb.codeplex.com/SourceControl/latest#trunk/Managed.AndroidDebugBridge/Managed.Adb.Tests/BaseDeviceTests.cs

namespace Org.SwerveRobotics.Tools.Library
    {
    /// <summary>
    /// ADB manager manages our access to the Android ADB utility
    /// </summary>
    class ADBManager
        {
        string thisProcessDirectory()
        // Return the name of the directory in which our .EXE is found
            {
            using (ProcessModule exe = Process.GetCurrentProcess().MainModule)
                {
                return Path.GetDirectoryName(exe.FileName);
                }
            }
        string adbExePath()
            {
            return Path.Combine(this.thisProcessDirectory(), "adb.exe");
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    public static class ProcessUtil
    // A class with various utilities for manipulating processes
        {
        static void StartRedirected(this Process process, string exeName)
            {
            ProcessStartInfo info = new ProcessStartInfo(exeName);
            info.UseShellExecute        = false;
            info.RedirectStandardInput  = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError  = true;
            info.CreateNoWindow         = true;
            process.StartInfo = info;

            process.Start();
            }
        }
    }

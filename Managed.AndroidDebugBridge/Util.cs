using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Managed.Adb
    {
    static class Util
        {
        public static bool equalsIgnoreCase(string me, string him)
            {
            return me.ToLowerInvariant() == him.ToLowerInvariant();
            }
        public static bool equals(string me, string him)
            {
            return me == him;
            }

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool CloseHandle(IntPtr handle);

        public static bool ConsoleTracingEnabled = false;
        public static string TraceTag = "BlueBotBug: ";

        public static void ConsoleTraceError(string format, params object[] arguments)
            {
            if (ConsoleTracingEnabled) System.Console.WriteLine(format, arguments);
            Trace(format, arguments);
            }
        public static void Trace(string format, params object[] arguments)
            {
            string s = string.Format(format, arguments);
            System.Diagnostics.Debug.WriteLine($"{TraceTag}{s}");
            }
        public static void ConsoleTraceError(object o)
            {
            if (ConsoleTracingEnabled) System.Console.WriteLine(o);
            Trace(o);
            }
        public static void Trace(object o)
            {
            string s = o.ToString();
            System.Diagnostics.Debug.WriteLine($"{TraceTag}{s}");
            }

        public static bool FileExists(string fileName)
            {
            return fileName != null && (new FileInfo(fileName)).Exists;
            }
        }
    }

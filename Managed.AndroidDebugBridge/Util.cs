using System;
using System.Collections.Generic;
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

        public static void ConsoleTrace(string format, params object[] arguments) { if (ConsoleTracingEnabled) System.Console.WriteLine(format, arguments); }
        public static void ConsoleTrace<T>(T t)                                   { if (ConsoleTracingEnabled) System.Console.WriteLine(t); }
        }
    }

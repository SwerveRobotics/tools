using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static Managed.Adb.Util;

namespace Managed.Adb
    {
    public sealed class AndroidDebugBridge
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        public  static  IPEndPoint  SocketAddress   { get; private set; }
        public  static  IPAddress   HostAddress     { get; }
        public  const   string      ADB_EXE         = "adb.exe";
        public  const   string      DDMS            = "monitor.bat";
        public  const   int         ADB_PORT        = 5037;

        public          event EventHandler<DeviceEventArgs>     DeviceConnected;
        public          event EventHandler<DeviceEventArgs>     DeviceDisconnected;
        public          event EventHandler<AndroidDebugBridge>  ServerStarted;
        public          event EventHandler<AndroidDebugBridge>  ServerKilled;

        private const   int         ADB_VERSION_MICRO_MIN = 20;
        private const   int         ADB_VERSION_MICRO_MAX = -1;
        private const   string      ADB_VERSION_PATTERN = "^.*(\\d+)\\.(\\d+)\\.(\\d+)$";

        private         string      pathToAdbExe;
        public          DeviceTracker deviceTracker;
        private         bool        versionCheckMatched;

        /**
         * Returns the full pathname of the ADB executable.
         *
         * @exception   FileNotFoundException   thrown when the we can't find ADB by any of our strategies.
         *
         * @return  the path to adb.exe.
         */
        public static string AdbPath
            {
            get
                {
                // Find the Android SDK, looking in a number of places trying to find it.
                // Once found, look in platform-tools therein
                string sdk = null;
                if (sdk == null) sdk = ReadRegistry(@"SOFTWARE\Android Studio",    @"SdkPath");
                if (sdk == null) sdk = ReadRegistry(@"SOFTWARE\Android SDK Tools", @"Path");
                if (sdk == null) sdk = Environment.GetEnvironmentVariable(@"ANDROID_SDK_HOME");
                if (sdk != null)
                    {
                    string file = Path.Combine(sdk, @"platform-tools", ADB_EXE);
                    if (FileExists(file))
                        return file;
                    }

                // Use the one that shipped with our code
                string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                if (dir != null)
                    {
                    string file = Path.Combine(dir, ADB_EXE);
                    if (FileExists(file))
                        return file;
                    }

                // Can't find it
                throw new FileNotFoundException($"unable to locate '{ADB_EXE}'");
                }
            }

        private static string ReadRegistry(string path, string valueName)
            {
            // Look in both 32 bit and 64 bit variations of the path, explicitly, as what
            // we would see implicitly would be highly sensitive to various code compilation issues.
            return ReadRegistry(true, path, valueName) ?? ReadRegistry(false, path, valueName);
            }

        private static string ReadRegistry(bool is64, string path, string valueName)
            {
            string result = null;
            IntPtr HKEY_LOCAL_MACHINE = (IntPtr)AsInt(0x80000002);
            using (SafeRegistryHandle hkeyRootNativeHandle = new SafeRegistryHandle(HKEY_LOCAL_MACHINE, false))
                {
                using (RegistryKey hkeyRoot = RegistryKey.FromHandle(hkeyRootNativeHandle, is64 ? RegistryView.Registry64 : RegistryView.Registry32))
                    {
                    using (RegistryKey key = hkeyRoot.OpenSubKey(path))
                        {
                        result = key?.GetValue(valueName) as string;
                        }
                    }
                }
            return result;
            }

        private static int AsInt(uint u) { return unchecked((int)u); }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW", SetLastError = true)]
        private static extern int RegOpenKeyExW(IntPtr hKey, string subKey, uint options, int sam, out IntPtr phkResult);

        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        /**
         * Instantiates a new bridge, using a particular ADB executable.
         * 
         * @exception   FileNotFoundException   thrown if the indicated path does not currently exist.
         *
         * @param   pathToAdbExe    The path to adb executable.
         */
        public AndroidDebugBridge(string pathToAdbExe)
            {
            if (!File.Exists(pathToAdbExe))
                {
                ConsoleTraceError(pathToAdbExe);
                throw new FileNotFoundException("unable to locate adb in the specified location");
                }

            this.pathToAdbExe = pathToAdbExe;
            CheckAdbVersion();
            }

        public AndroidDebugBridge() : this(AdbPath)
            {
            }

        public static AndroidDebugBridge Create(string pathToAdbExe)
            {
            AndroidDebugBridge result = new AndroidDebugBridge(pathToAdbExe);
            if (!result.StartTracking())
                {
                result = null;
                }
            return result;
            }

        public static AndroidDebugBridge Create()
            {
            return Create(AdbPath);
            }

        static AndroidDebugBridge()
        // static/class initialiation
            {
            try
                {
                HostAddress     = IPAddress.Loopback;
                SocketAddress   = new IPEndPoint(HostAddress, ADB_PORT);
                }
            catch (ArgumentOutOfRangeException)
                {
                }
            }

        //---------------------------------------------------------------------------------------
        // Initialization
        //---------------------------------------------------------------------------------------

        internal void OnDeviceConnected(DeviceEventArgs e)    => this.DeviceConnected?.Invoke(null, e);
        internal void OnDeviceDisconnected(DeviceEventArgs e) => this.DeviceDisconnected?.Invoke(null, e);
        internal void OnServerStarted()                       => this.ServerStarted?.Invoke(null, this);
        internal void OnServerKilled()                        => this.ServerKilled?.Invoke(null, this);

        public bool StartTracking()
            {
            if (!this.versionCheckMatched)
                return false;

            if (!StartServer())
                return false;

            this.deviceTracker = new DeviceTracker(this);
            this.deviceTracker.StartDeviceTracking();

            return true;
            }

        public void StopTracking()
            {
            if (this.deviceTracker != null)
                {
                this.deviceTracker.StopDeviceTracking();
                this.deviceTracker = null;
                }
            }

        /** Queries ADB for its version number and checks it against #MIN_VERSION_NUMBER and MAX_VERSION_NUMBER. */
        private void CheckAdbVersion()
            {
            // default is bad check
            this.versionCheckMatched = false;
            try
                {
                Log.d(DDMS, $"Checking '{this.pathToAdbExe} version'");

                ProcessStartInfo psi = new ProcessStartInfo(this.pathToAdbExe, "version");
                psi.WindowStyle             = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow          = true;
                psi.UseShellExecute         = false;
                psi.RedirectStandardError   = true;
                psi.RedirectStandardOutput  = true;

                List<string> errorOutput = new List<string>();
                List<string> stdOutput = new List<string>();
                using (Process proc = Process.Start(psi))
                    {
                    int status = GrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                    if (status != 0)
                        {
                        StringBuilder builder = new StringBuilder("'adb version' failed!");
                        builder.AppendLine(string.Empty);
                        foreach (string error in errorOutput)
                            {
                            builder.AppendLine(error);
                            }
                        Log.LogAndDisplay(LogLevel.Error, "adb", builder.ToString());
                        }
                    }

                // check both stdout and stderr
                bool versionFound = false;
                foreach (string line in stdOutput)
                    {
                    versionFound = ScanVersionLine(line);
                    if (versionFound)
                        break;
                    }

                if (!versionFound)
                    {
                    foreach (string line in errorOutput)
                        {
                        versionFound = ScanVersionLine(line);
                        if (versionFound)
                            break;
                        }
                    }

                if (!versionFound)
                    {
                    // if we get here, we failed to parse the output.
                    Log.LogAndDisplay(LogLevel.Error, ADB_EXE, "Failed to parse the output of 'adb version'");
                    }
                }
            catch (IOException e)
                {
                Log.LogAndDisplay(LogLevel.Error, ADB_EXE, "Failed to get the adb version: " + e.Message);
                }
            }

        /**
         * Scans a line resulting from 'adb version' for a potential version number. If a version number
         * is found, it checks the version number against what is expected by this version of ddms.
         *
         * @param   line    The line to scan.
         *
         * @return  true if a version number was found (whether it is acceptable or not).
         */

        private bool ScanVersionLine(string line)
            {
            if (!string.IsNullOrEmpty(line))
                {
                Match matcher = Regex.Match(line, ADB_VERSION_PATTERN);
                if (matcher.Success)
                    {
                    int majorVersion = int.Parse(matcher.Groups[1].Value);
                    int minorVersion = int.Parse(matcher.Groups[2].Value);
                    int microVersion = int.Parse(matcher.Groups[3].Value);

                    // check only the micro version for now.
                    if (microVersion < ADB_VERSION_MICRO_MIN)
                        {
                        string message = string.Format("Required minimum version of adb: {0}.{1}.{2}. Current version is {0}.{1}.{3}", majorVersion, minorVersion, ADB_VERSION_MICRO_MIN, microVersion);
                        Log.LogAndDisplay(LogLevel.Error, ADB_EXE, message);
                        }
                    else if (ADB_VERSION_MICRO_MAX != -1 && microVersion > ADB_VERSION_MICRO_MAX)
                        {
                        string message = string.Format("Required maximum version of adb: {0}.{1}.{2}. Current version is {0}.{1}.{3}",
                            majorVersion, minorVersion, ADB_VERSION_MICRO_MAX, microVersion);
                        Log.LogAndDisplay(LogLevel.Error, ADB_EXE, message);
                        }
                    else
                        {
                        this.versionCheckMatched = true;
                        }
                    return true;
                    }
                }
            return false;
            }

        public bool StartServer()
            {
            int status = -1;

            try
                {
                string command = "start-server";
                ProcessStartInfo psi = new ProcessStartInfo(this.pathToAdbExe, command);
                psi.CreateNoWindow          = true;
                psi.WindowStyle             = ProcessWindowStyle.Hidden;
                psi.UseShellExecute         = false;
                psi.RedirectStandardError   = true;
                psi.RedirectStandardOutput  = true;

                using (Process proc = Process.Start(psi))
                    {
                    List<string> errorOutput = new List<string>();
                    List<string> stdOutput   = new List<string>();
                    status = GrabProcessOutput(proc, errorOutput, stdOutput, false /* waitForReaders */);
                    }
                }
            catch (IOException ioe)
                {
                Log.d(DDMS, "Unable to run 'adb': {0}", ioe.Message);
                }
            catch (ThreadInterruptedException ie)
                {
                Log.d(DDMS, "Unable to run 'adb': {0}", ie.Message);
                }
            catch (Exception e)
                {
                Log.e(DDMS, e);
                }

            if (status != 0)
                {
                Log.w(DDMS, "'adb start-server' failed -- run manually if necessary");
                return false;
                }

            OnServerStarted();
            Log.d(DDMS, "'adb start-server' succeeded");
            return true;
            }

        public bool KillServer()
            {
            int status = -1;

            try
                {
                string command = "kill-server";
                ProcessStartInfo psi                     = new ProcessStartInfo(this.pathToAdbExe, command);
                psi.CreateNoWindow          = true;
                psi.WindowStyle             = ProcessWindowStyle.Hidden;
                psi.UseShellExecute         = false;
                psi.RedirectStandardError   = true;
                psi.RedirectStandardOutput  = true;

                using (Process proc = Process.Start(psi))
                    {
                    // We are conservative in our kill notifications: we don't KNOW that it got
                    // killed, but we know that we tried.
                    OnServerKilled();
                    proc.WaitForExit();
                    status = proc.ExitCode;
                    }
                }
            catch (IOException)
                {
                // we'll return false;
                }
            catch (Exception)
                {
                // we'll return false;
                }

            if (status != 0)
                {
                Log.w(DDMS, "'adb kill-server' failed -- run manually if necessary");
                return false;
                }

            Log.d(DDMS, "'adb kill-server' succeeded");
            return true;
            }

        /**
         * Get the stderr/stdout outputs of a process and return when the process is done. Both
         * <b>must</b> be read or the process will block on windows.
         *
         * @exception   ArgumentNullException   Thrown when one or more required arguments are null.
         *
         * @param   process         The process to get the ouput from.
         * @param   errorOutput     The array to store the stderr output. cannot be null.
         * @param   stdOutput       The array to store the stdout output. cannot be null.
         * @param   waitforReaders  if true, this will wait for the reader threads.
         *
         * @return  the process return code.
         */
        private int GrabProcessOutput(Process process, List<string> errorOutput, List<string> stdOutput, bool waitforReaders)
            {
            if (errorOutput == null)
                throw new ArgumentNullException("errorOutput");
            if (stdOutput == null)
                throw new ArgumentNullException("stdOutput");

            // read the lines as they come. if null is returned, it's
            // because the process finished
            Thread t1 = new Thread(new ThreadStart(delegate
                {
                // create a buffer to read the stdoutput
                try
                    {
                    using (StreamReader sr = process.StandardError)
                        {
                        while (!sr.EndOfStream)
                            {
                            string line = sr.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                                {
                                Log.e(ADB_EXE, line);
                                errorOutput.Add(line);
                                }
                            }
                        }
                    }
                catch (Exception)
                    {
                    // do nothing.
                    }
                }));

            Thread t2 = new Thread(new ThreadStart(delegate
                {
                // create a buffer to read the std output
                try
                    {
                    using (StreamReader sr = process.StandardOutput)
                        {
                        while (!sr.EndOfStream)
                            {
                            string line = sr.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                                {
                                stdOutput.Add(line);
                                }
                            }
                        }
                    }
                catch (Exception)
                    {
                    // do nothing.
                    }
                }));

            t1.Start();
            t2.Start();

            // it looks like on windows process#waitFor() can return
            // before the thread have filled the arrays, so we wait for both threads and the
            // process itself.
            if (waitforReaders)
                {
                try
                    {
                    t1.Join();
                    }
                catch (ThreadInterruptedException)
                    {
                    }
                try
                    {
                    t2.Join();
                    }
                catch (ThreadInterruptedException)
                    {
                    }
                }

            // get the return code from the process
            process.WaitForExit();
            return process.ExitCode;
            }

        //---------------------------------------------------------------------------------------
        // Historical
        //---------------------------------------------------------------------------------------

        /**
         * Initializes the ddm library, in one of two ways:
         * 
         * Mode 1: clientSupport== true
         *      The library monitors the devices and the applications running on them. It will connect
         *      to each application, as a debugger of sort, to be able to interact with them through
         *      JDWP packets.
         * 
         * Mode 2: clientSupport==false
         *      The library only monitors devices. The applications are left untouched, letting other
         *      tools built on ddmlib to connect a debugger to them.
         * 
         * Only one tool can run in mode 1 at the same time. Note that mode 1 does not prevent debugging
         * of applications running on devices. Mode 1 lets debuggers connect to ddmlib which acts as a
         * proxy between the debuggers and the applications to debug. See {@link
         * Client#getDebuggerListenPort()}. The preferences of ddmlib should also be initialized with
         * whatever default values were changed from the default values.
         *
         * @param   clientSupport   Indicates whether the library should enable the monitoring and
         *                          interaction with applications running on the devices.
         *                          
         *                          When the application quits, {@link #terminate()} should be called.
         *
         * @sa  AndroidDebugBridge#createBridge(String, boolean)
         * @sa  DdmPreferences
         */
        }
    }

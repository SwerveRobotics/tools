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
using Org.SwerveRobotics.Tools.ManagedADB.Exceptions;
using Org.SwerveRobotics.Tools.Util;
using static Org.SwerveRobotics.Tools.ManagedADB.Util;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public sealed class AndroidDebugBridge
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        public  static  IPEndPoint  AdbServerSocketAddress   { get; private set; }
        public  static  IPAddress   AdbServerHostAddress     { get; }
        public  const   int         AdbServerPort   = 5037;

        public  const   string      ADB_EXE         = "adb.exe";
        public  const   string      DDMS            = "monitor.bat";

        public          event EventHandler<DeviceEventArgs>              DeviceConnected;
        public          event EventHandler<DeviceEventArgs>              DeviceDisconnected;
        public          event EventHandler<AndroidDebugBridgeEventArgs>  ServerStartedOrReconnected;
        public          event EventHandler<AndroidDebugBridgeEventArgs>  ServerKilled;

        private const   string      ADB_VERSION_PATTERN = "^.*(\\d+)\\.(\\d+)\\.(\\d+)$";

        private         string        pathToAdbExe;
        private         Version       localAdbExeVersion;
        public          DeviceTracker deviceTracker;

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
                IntPtr HKEY_CURRENT_USER  = (IntPtr)AsInt(0x80000001);
                IntPtr HKEY_LOCAL_MACHINE = (IntPtr)AsInt(0x80000002);

                // Find the Android SDK, looking in a number of places trying to find it.
                // Once found, look in platform-tools therein
                string file = null;
                if (file == null) file = FindInSDK(HKEY_LOCAL_MACHINE, @"SOFTWARE\Android Studio",          @"SdkPath");
                if (file == null) file = FindInSDK(HKEY_LOCAL_MACHINE, @"SOFTWARE\Android SDK Tools",       @"Path");
                if (file == null) file = FindInSDK(HKEY_CURRENT_USER,  @"Software\Novell\Mono for Android", @"AndroidSdkDirectory");
                if (file == null) file = FindInSDK(Environment.GetEnvironmentVariable(@"ANDROID_SDK_HOME"));
                if (file != null)
                    return file;

                // Use the one that shipped with our code
                string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                if (dir != null)
                    {
                    file = Path.Combine(dir, ADB_EXE);
                    if (FileExists(file))
                        return file;
                    }

                // Can't find it
                throw new FileNotFoundException($"unable to locate '{ADB_EXE}'");
                }
            }

        private static string FindInSDK(IntPtr root, string path, string valueName)
            {
            return FindInSDK(ReadRegistry(root, path, valueName));
            }
        private static string FindInSDK(string sdk)
            {
            if (sdk != null)
                {
                string file = Path.Combine(sdk, @"platform-tools", ADB_EXE);
                if (FileExists(file))
                    return file;
                }
            return null;
            }

        private static string ReadRegistry(IntPtr root, string path, string valueName)
            {
            // Look in both 32 bit and 64 bit variations of the path, explicitly, as what
            // we would see implicitly would be highly sensitive to various code compilation issues.
            return ReadRegistry(true, root, path, valueName) ?? ReadRegistry(false, root, path, valueName);
            }

        private static string ReadRegistry(bool is64, IntPtr root, string path, string valueName)
            {
            string result = null;
            using (SafeRegistryHandle hkeyRootNativeHandle = new SafeRegistryHandle(root, false))
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

            this.pathToAdbExe       = pathToAdbExe;
            this.localAdbExeVersion = CheckLocalAdbExeVersion(this.pathToAdbExe);
            }

        public AndroidDebugBridge() : this(AdbPath)
            {
            }

        // throw on failure
        public static AndroidDebugBridge Create(string pathToAdbExe)
            {
            AndroidDebugBridge result = new AndroidDebugBridge(pathToAdbExe);
            result.StartTracking();
            return result;
            }

        // throw on failure
        public static AndroidDebugBridge Create()
            {
            return Create(AdbPath);
            }

        static AndroidDebugBridge()
        // static/class initialiation
            {
            try
                {
                AdbServerHostAddress     = IPAddress.Loopback;
                AdbServerSocketAddress   = new IPEndPoint(AdbServerHostAddress, AdbServerPort);
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
        internal void OnEnsureServerStarted()                 => this.ServerStartedOrReconnected?.Invoke(null, new AndroidDebugBridgeEventArgs(this));
        internal void OnServerKilled()                        => this.ServerKilled?.Invoke(null, new AndroidDebugBridgeEventArgs(this));

        // throws on failure
        public void StartTracking()
            {
            if (this.localAdbExeVersion == null)
                throw new InvalidADBVersionException();

            EnsureSeverStartedVersion(this.localAdbExeVersion.Server);

            this.deviceTracker = new DeviceTracker(this);
            this.deviceTracker.StartDeviceTracking();
            }

        // never throws
        public void StopTracking()
            {
            if (this.deviceTracker != null)
                {
                this.deviceTracker.StopDeviceTracking();
                this.deviceTracker = null;
                }
            }

        /**
         * Queries ADB for its version number and checks it against Version.Required. Note that this checks the LOCAL ADB
         * version number, not the version number of the ADB server which may happen to already be running. If an accept
         * 
         * Returns the version of ADB, or null if that can't be found or isn't acceptable.
         * 
         * Does not throw.
         */
        private static Version CheckLocalAdbExeVersion(string pathToAdbExe)
            {
            Version result = null;
            try
                {
                Log.d(DDMS, $"Checking '{pathToAdbExe} version'");

                ProcessStartInfo psi = new ProcessStartInfo(pathToAdbExe, "version");
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
                    versionFound = FindAndCheckVersionLine(line, out result);
                    if (versionFound)
                        break;
                    }

                if (!versionFound)
                    {
                    foreach (string line in errorOutput)
                        {
                        versionFound = FindAndCheckVersionLine(line, out result);
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
            catch (Exception e)
                {
                Log.LogAndDisplay(LogLevel.Error, ADB_EXE, $"exception checking ADB version: {e.Message}");
                }

            return result;
            }

        /**
         * Scans a line resulting from 'adb version' for a potential version number. If a version number is found, it checks
         * the version number against what is expected by this version of ddms.
         * 
         * @param   line    The line to scan.
         *
         * @return  true if a version number was found (whether it is acceptable or not).
         */
        private static bool FindAndCheckVersionLine(string line, out Version versionOk)
            {
            versionOk = null;

            if (!string.IsNullOrEmpty(line))
                {
                Match matcher = Regex.Match(line, ADB_VERSION_PATTERN);
                if (matcher.Success)
                    {
                    int majorVersion = int.Parse(matcher.Groups[1].Value);
                    int minorVersion = int.Parse(matcher.Groups[2].Value);
                    int microVersion = int.Parse(matcher.Groups[3].Value);

                    Version version = new Version(majorVersion, minorVersion, microVersion);

                    if (version < Version.Required)
                        {
                        string message = $"Required minimum version of adb: {Version.Required}. Current version is {version}";
                        Log.LogAndDisplay(LogLevel.Error, ADB_EXE, message);
                        }
                    else
                        {
                        versionOk = version;
                        }
                    return true;
                    }
                }
            return false;
            }

        // throws on failure
        public void EnsureSeverStartedVersion(int versionRequired)
            {
            // If it's the wrong server version, restart, once
            EnsureServerStarted();
            int serverVersion = AdbHelper.Instance.GetAdbServerVersion(AdbServerSocketAddress);
            if (serverVersion != versionRequired)
                {
                KillServer();
                EnsureServerStarted();
                }
            }

        /**
         * Ensures that the server is started. This will check for the presence of the ADB server
         * and if absent cause it to start running.
         * 
         * Throws on failure
         */
        public void EnsureServerStarted()
            {
            Log.d(DDMS, "EnsureServerStarted() ...");

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
                    int exitCode = GrabProcessOutput(proc, errorOutput, stdOutput, false /* waitForReaders */);
                    if (exitCode != 0)
                        throw new ProcessErrorExitException(exitCode);

                    // TODO (maybe): Check response?
                    }
                }
            catch (Exception e)
                {
                Log.e(DDMS, $"exception in EnsureServerStarted: {e}");
                throw;
                }

            Log.d(DDMS, "... EnsureServerStarted() succeeded");
            OnEnsureServerStarted();
            }

        public bool KillServer()
            {
            int status = -1;
            Log.d(DDMS, "'adb kill-server' ...");

            try
                {
                string command = "kill-server";
                ProcessStartInfo psi = new ProcessStartInfo(this.pathToAdbExe, command);
                psi.CreateNoWindow          = true;
                psi.WindowStyle             = ProcessWindowStyle.Hidden;
                psi.UseShellExecute         = false;
                psi.RedirectStandardError   = true;
                psi.RedirectStandardOutput  = true;

                using (Process proc = Process.Start(psi))
                    {
                    // We are conservative in our kill notifications: we don't KNOW that it got killed, but we know that we tried.
                    proc.WaitForExit();
                    status = proc.ExitCode;
                    OnServerKilled();
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

            Log.d(DDMS, "... 'adb kill-server' succeeded");
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
        private static int GrabProcessOutput(Process process, List<string> errorOutput, List<string> stdOutput, bool waitforReaders)
            {
            if (errorOutput == null)
                throw new ArgumentNullException(nameof(errorOutput));
            if (stdOutput == null)
                throw new ArgumentNullException(nameof(stdOutput));

            // read the lines as they come. if null is returned, it's
            // because the process finished
            HandshakeThreadStarter t1 = new HandshakeThreadStarter("StdErr reader", (starter) =>
                {
                // create a buffer to read the stdoutput
                try
                    {
                    using (StreamReader sr = process.StandardError)
                        {
                        starter.ThreadHasStarted();

                        while (!starter.StopRequested && !sr.EndOfStream)
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
                });

            HandshakeThreadStarter t2 = new HandshakeThreadStarter("StdOut reader", (starter) =>
                {
                // create a buffer to read the std output
                try
                    {
                    using (StreamReader sr = process.StandardOutput)
                        {
                        starter.ThreadHasStarted();

                        while (!starter.StopRequested && !sr.EndOfStream)
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
                });

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
        // Types
        //---------------------------------------------------------------------------------------
        
        public class Version
            {
            public readonly int Major;      // See ADB_VERSION_MAJOR  in Android sources
            public readonly int Minor;      // See ADB_VERSION_MINOR  in Android sources
            public readonly int Server;     // See ADB_SERVER_VERSION in Android sources

            public static Version Required = new Version(1, 0, 32);

            public Version(int major, int minor, int server)
                {
                this.Major = major;
                this.Minor = minor;
                this.Server = server;
                }

            public static bool operator >  (Version left, Version right) => !(right < left);
            public static bool operator >= (Version left, Version right) => !(right <= left);
            public static bool operator <  (Version left, Version right) => left.CompareLexicographically(right) < 0;
            public static bool operator <= (Version left, Version right) => left.CompareLexicographically(right) <= 0;

            public int CompareLexicographically(Version him)
                {
                if (this.Major < him.Major)
                    return -1;
                else if (this.Major == him.Major)
                    {
                    if (this.Minor < him.Minor)
                        return -1;
                    else if (this.Minor == him.Minor)
                        {
                        return this.Server - him.Server;
                        }
                    else 
                        return 1;
                    }
                else 
                    return 1;
                }

            public override string ToString()
                {
                return $"{Major}.{Minor}.{this.Server}";
                }
            public override bool Equals(object obj)
                {
                return (obj is Version) && (this.CompareLexicographically(obj as Version) == 0);
                }
            public override int GetHashCode()
                {
                return Major.GetHashCode() ^ Minor.GetHashCode() ^ this.Server.GetHashCode();
                }
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

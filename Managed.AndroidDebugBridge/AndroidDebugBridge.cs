using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using static Managed.Adb.Util;

namespace Managed.Adb
    {
    /// <summary>
    /// The android debug bridge
    /// </summary>
    public sealed class AndroidDebugBridge
        {
        /** Occurs when [device connected] */
        public event EventHandler<DeviceEventArgs> DeviceConnected;

        /** Occurs when [device disconnected]. */
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;

        /** Minimum version number of adb supported. See //device/tools/adb/adb.h */
        private const int ADB_VERSION_MICRO_MIN = 20;

        /** Maximum versionnumber of adb supported. See //device/tools/adb/adb.h */
        private const int ADB_VERSION_MICRO_MAX = -1;

        /** The regex pattern for getting the adb version. */
        private const string ADB_VERSION_PATTERN = "^.*(\\d+)\\.(\\d+)\\.(\\d+)$";

        /// <summary>
        /// The ADB executive
        /// </summary>
        public const string ADB = "adb.exe";
        /// <summary>
        /// The DDMS executive
        /// </summary>
        public const string DDMS = "monitor.bat";
        /// <summary>
        /// The hierarchy viewer
        /// </summary>
        public const string HIERARCHYVIEWER = "hierarchyviewer.bat";
        /// <summary>
        /// The AAPT executive
        /// </summary>
        public const string AAPT = "aapt.exe";


        // Where to find the ADB bridge.
        /// <summary>
        /// The default ADB bridge port
        /// </summary>
        public const int ADB_PORT = 5037;

        /** Filename of the adb executable */
        public const string ADB_EXE = "ADB.EXE";

        /**
         * Returns the full pathname of the ADB executable.
         *
         * @exception   FileNotFoundException   thrown when the we can't find ADB by any of our strategies
         *
         * @return  the path to adb.exe
         */
        public static string AdbPath
            {
            get
                {
                // Find the Android SDK, look in platform-tools therein
                // Look for the sdk fist in the Android Studio configuration, then in an environment variable
                string sdk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Android Studio")?.GetValue(@"SdkPath") as string;
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
        


        private static AndroidDebugBridge g_instance;

        /**
         * Gets or sets the socket address
         */
        public static IPEndPoint SocketAddress
            {
            get; private set;
            }

        /**
         * Gets or sets the host address.
         */
        public static IPAddress HostAddress
            {
            get; private set;
            }

        /** Initializes the <see cref="AndroidDebugBridge"/> class. */
        static AndroidDebugBridge()
            {
            // built-in local address/port for ADB.
            try
                {
                HostAddress   = IPAddress.Loopback;
                SocketAddress = new IPEndPoint(HostAddress, ADB_PORT);
                }
            catch (ArgumentOutOfRangeException)
                {
                }
            }

        /*
		 * Initializes the <code>ddm</code> library.
		 * <p/>This must be called once <b>before</b> any call to
		 * {@link #createBridge(String, boolean)}.
		 * <p>The library can be initialized in 2 ways:
		 * <ul>
		 * <li>Mode 1: <var>clientSupport</var> == <code>true</code>.<br>The library monitors the
		 * devices and the applications running on them. It will connect to each application, as a
		 * debugger of sort, to be able to interact with them through JDWP packets.</li>
		 * <li>Mode 2: <var>clientSupport</var> == <code>false</code>.<br>The library only monitors
		 * devices. The applications are left untouched, letting other tools built on
		 * <code>ddmlib</code> to connect a debugger to them.</li>
		 * </ul>
		 * <p/><b>Only one tool can run in mode 1 at the same time.</b>
		 * <p/>Note that mode 1 does not prevent debugging of applications running on devices. Mode 1
		 * lets debuggers connect to <code>ddmlib</code> which acts as a proxy between the debuggers and
		 * the applications to debug. See {@link Client#getDebuggerListenPort()}.
		 * <p/>The preferences of <code>ddmlib</code> should also be initialized with whatever default
		 * values were changed from the default values.
		 * <p/>When the application quits, {@link #terminate()} should be called.
		 * @param clientSupport Indicates whether the library should enable the monitoring and
		 * interaction with applications running on the devices.
		 * @see AndroidDebugBridge#createBridge(String, boolean)
		 * @see DdmPreferences
		 */

        /**
         * Initializes the ddm library, in one of two ways
         * 
		 * Mode 1: clientSupport== true
         *      The library monitors the devices and the applications running on them. It will connect to each 
		 *      application, as a debugger of sort, to be able to interact with them through JDWP packets.
         * 
         * Mode 2: clientSuppor==false
         *      The library only monitors devices. The applications are left untouched, letting other tools built on
		 *      ddmlib to connect a debugger to them.
         *
         * @param   clientSupport   true if there is client support, false otherwise
         *                          
		 * Only one tool can run in mode 1 at the same time. Note that mode 1 does not prevent debugging of 
         * applications running on devices. Mode 1 lets debuggers connect to ddmlib which acts as a proxy between 
         * the debuggers and the applications to debug. See {@link Client#getDebuggerListenPort()}. The preferences of 
         * ddmlib should also be initialized with whatever default values were changed from the default values.
         * 
		 * When the application quits, {@link #terminate()} should be called.
         * 
		 * @param clientSupport Indicates whether the library should enable the monitoring and
		 * interaction with applications running on the devices.
		 * @see AndroidDebugBridge#createBridge(String, boolean)
		 * @see DdmPreferences

         */
        public static void Initialize(bool clientSupport)
            {
            ClientSupport = clientSupport;
            }

        /** Terminates the ddm library. This must be called upon application termination. */
        public static void Terminate()
            {
            // kill the monitoring services
            if (Instance != null && Instance.DeviceMonitor != null)
                {
                Instance.DeviceMonitor.Stop();
                Instance.DeviceMonitor = null;
                }
            }

        public static AndroidDebugBridge Instance
            {
            get
                {
                if (g_instance == null)
                    {
                    g_instance = OpenBridge();
                    }
                return g_instance;
                }
            }

        public static AndroidDebugBridge Bridge
            {
            get
                {
                return Instance;
                }
            }

        public static bool ClientSupport
            {
            get; private set;
            }



        /// <summary>
        /// Creates a {@link AndroidDebugBridge} that is not linked to any particular executable.
        /// This bridge will expect adb to be running. It will not be able to start/stop/restart
        /// adb.
        /// If a bridge has already been started, it is directly returned with no changes
        /// </summary>
        /// <returns></returns>
        public static AndroidDebugBridge OpenBridge()
            {
            if (g_instance != null)
                {
                return g_instance;
                }

            g_instance = new AndroidDebugBridge();
            try
                {
                g_instance.Start();
                }
            catch (ArgumentException)
                {
                g_instance = null;
                }

            return g_instance;
            }

        /**
         * Creates a new debug bridge from the location of the command line tool.
         *
         * @param   pathToAdbExe    the location of the command line tool 'adb'.
         *
         * @return  a connected bridge.
         */
        public static AndroidDebugBridge OpenBridge(string pathToAdbExe)
            {
            if (g_instance != null)
                {
                if (!string.IsNullOrEmpty(PathToAdbExe) && Util.equalsIgnoreCase(PathToAdbExe, pathToAdbExe))
                    {
                    return g_instance;
                    }

                // stop the current server
                Util.ConsoleTraceError("stopping current ADB server");
                g_instance.Stop();
                }

            g_instance = new AndroidDebugBridge(pathToAdbExe);
            try
                {
                g_instance.Start();
                }
            catch (Exception)
                {
                g_instance = null;
                throw;
                }

            return g_instance;
            }

        /// <summary>
        /// Disconnects the current debug bridge, and destroy the object.
        /// </summary>
        /// <remarks>This also stops the current adb host server.</remarks>
        public static void CloseBridge()
            {
            if (g_instance != null)
                {
                g_instance.Stop();
                g_instance = null;
                }
            }

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <returns></returns>
        public static object GetLock()
            {
            return Instance;
            }

        /// <summary>
        /// Creates a new bridge.
        /// </summary>
        /// <param name="pathToAdbExe">the location of the command line tool</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private AndroidDebugBridge(string pathToAdbExe)
            {
            if (string.IsNullOrEmpty(pathToAdbExe))
                {
                throw new ArgumentException();
                }

            if (!File.Exists(pathToAdbExe))
                {
                Util.ConsoleTraceError(pathToAdbExe);
                throw new FileNotFoundException("unable to locate adb in the specified location");
                }

            PathToAdbExe = pathToAdbExe;

            CheckAdbVersion();
            }

        /// <summary>
        /// Creates a new bridge not linked to any particular adb executable.
        /// </summary>
        private AndroidDebugBridge()
            {
            }

        internal void OnDeviceConnected(DeviceEventArgs e)
            {
            this.DeviceConnected?.Invoke(this, e);
            }

        internal void OnDeviceDisconnected(DeviceEventArgs e)
            {
            this.DeviceDisconnected?.Invoke(this, e);
            }

        #region public methods

        /**
         * Starts the ADB server
         *
         * @return  true if it succeeds, false if it fails.
         */
        public bool Start()
            {
            if (string.IsNullOrEmpty(PathToAdbExe) || !this.DoVersionCheck || !StartAdb())
                {
                return false;
                }

            this.Started = true;

            // now that the bridge is connected, we start the underlying services.
            DeviceMonitor = new DeviceMonitor(this);
            DeviceMonitor.Start();

            return true;
            }

        /**
         * Kills the debug bridge, and the adb host server.
         *
         * @return  <c>true</c> if success.
         */
        private bool Stop()
            {
            if (!StopMonitoring())
                return false;

            if (!KillAdb())
                return false;

            this.Started = false;
            return true;
            }

        public bool StopMonitoring()
            {
            // if we haven't started we return false;
            if (!Started)
                {
                return false;
                }

            // kill the monitoring services
            if (DeviceMonitor != null)
                {
                DeviceMonitor.Stop();
                DeviceMonitor = null;
                }

            return true;
            }

        /// <summary>
        /// Restarts adb, but not the services around it.
        /// </summary>
        /// <returns><c>true</c> if success.</returns>
        public bool Restart()
            {
            if (string.IsNullOrEmpty(PathToAdbExe))
                {
                Log.e(ADB, "Cannot restart adb when AndroidDebugBridge is created without the location of adb.");
                return false;
                }

            if (!this.DoVersionCheck)
                {
                Log.LogAndDisplay(LogLevel.Error, ADB, "Attempting to restart adb, but version check failed!");
                return false;
                }
            lock (this)
                {
                KillAdb();

                bool restart = StartAdb();

                if (restart && DeviceMonitor == null)
                    {
                    DeviceMonitor = new DeviceMonitor(this);
                    DeviceMonitor.Start();
                    }

                return restart;
                }
            }
        #endregion

        #region public properties

        /// <summary>
        /// Gets or Sets the adb location on the OS.
        /// </summary>
        /// <value>The adb location on the OS.</value>
        public static string PathToAdbExe
            {
            get; set;
            }
        /// <summary>
        /// Gets the devices.
        /// </summary>
        /// <value>The devices.</value>
        public IList<Device> Devices
            {
            get
                {
                //if ( DeviceMonitor != null ) {
                //	return DeviceMonitor.Devices;
                //}
                //return new List<Device> ( );
                return AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);
                }
            }

        /// <summary>
        /// Returns whether the bridge has acquired the initial list from adb after being created.
        /// </summary>
        /// <remarks>
        /// <p/>Calling getDevices() right after createBridge(String, boolean) will
        /// generally result in an empty list. This is due to the internal asynchronous communication
        /// mechanism with <code>adb</code> that does not guarantee that the IDevice list has been
        /// built before the call to getDevices().
        /// <p/>The recommended way to get the list of IDevice objects is to create a
        /// IDeviceChangeListener object.
        /// </remarks>
        /// <returns>
        /// 	<c>true</c> if [has initial device list]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasInitialDeviceList()
            {
            if (DeviceMonitor != null)
                {
                return DeviceMonitor.HasInitialDeviceList;
                }
            return false;
            }

        /// <summary>
        /// Gets or sets the client to accept debugger connection on the custom "Selected debug port".
        /// </summary>
        /// <remarks>Not Yet Implemented</remarks>
        public IClient SelectedClient
            {
            get
                {
                /*MonitorThread monitorThread = MonitorThread.Instance;
				if ( monitorThread != null ) {
					return monitorThread.SelectedClient = selectedClient;
				}*/
                return null;
                }
            set
                {
                /*MonitorThread monitorThread = MonitorThread.Instance;
				if ( monitorThread != null ) {
					monitorThread.SelectedClient = value;
				}*/
                }
            }
        /// <summary>
        /// Returns whether the AndroidDebugBridge object is still connected to the adb daemon (server?).
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
            {
            get
                {
                //MonitorThread monitorThread = MonitorThread.Instance;
                if (DeviceMonitor != null /* && monitorThread != null */ )
                    {
                    return DeviceMonitor.IsMonitoring /* && monitorThread.State != State.TERMINATED*/;
                    }
                return false;
                }
            }

        /// <summary>
        /// Returns the number of times the AndroidDebugBridge object attempted to connect
        /// </summary>
        /// <value>The connection attempt count.</value>
        public int ConnectionAttemptCount
            {
            get
                {
                if (DeviceMonitor != null)
                    {
                    return DeviceMonitor.ADBConnectionAttempts;
                    }
                return -1;
                }
            }

        /**
         * Returns the number of times the AndroidDebugBridge object attempted to restart the adb daemon.
         *
         * @return  The number of restart attempts.
         */
        public int RestartAttemptCount
            {
            get
                {
                if (DeviceMonitor != null)
                    {
                    return DeviceMonitor.BridgeRestartAttempts;
                    }
                return -1;
                }
            }

        /// <summary>
        /// Gets the device monitor
        /// </summary>
        public DeviceMonitor DeviceMonitor
            {
            get; private set;
            }

        /** Gets or sets if the adb host has started. */
        private bool Started
            {
            get; set;
            }

        /** Whether or not we should version check the ADB instance we run */
        private bool DoVersionCheck
            {
            get; set;
            }

        /// <summary>
        /// Queries adb for its version number and checks it against #MIN_VERSION_NUMBER and MAX_VERSION_NUMBER
        /// </summary>
        private void CheckAdbVersion()
            {
            // default is bad check
            this.DoVersionCheck = false;

            if (string.IsNullOrEmpty(PathToAdbExe))
                {
                Util.ConsoleTraceError("AdbOsLocation is Empty");
                return;
                }

            try
                {
                Log.d(DDMS, $"Checking '{PathToAdbExe} version'");

                ProcessStartInfo psi = new ProcessStartInfo(PathToAdbExe, "version");
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

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
                        {
                        break;
                        }
                    }

                if (!versionFound)
                    {
                    foreach (string line in errorOutput)
                        {
                        versionFound = ScanVersionLine(line);
                        if (versionFound)
                            {
                            break;
                            }
                        }
                    }

                if (!versionFound)
                    {
                    // if we get here, we failed to parse the output.
                    Log.LogAndDisplay(LogLevel.Error, ADB, "Failed to parse the output of 'adb version'");
                    }

                }
            catch (IOException e)
                {
                Log.LogAndDisplay(LogLevel.Error, ADB, "Failed to get the adb version: " + e.Message);
                }
            }

        /// <summary>
        /// Scans a line resulting from 'adb version' for a potential version number.
        /// </summary>
        /// <param name="line">The line to scan.</param>
        /// <returns><c>true</c> if a version number was found (whether it is acceptable or not).</returns>
        /// <remarks>If a version number is found, it checks the version number against what is expected
        /// by this version of ddms.</remarks>
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
                        string message = string.Format("Required minimum version of adb: {0}.{1}.{2}. Current version is {0}.{1}.{3}",
                                        majorVersion, minorVersion, ADB_VERSION_MICRO_MIN, microVersion);
                        Log.LogAndDisplay(LogLevel.Error, ADB, message);
                        }
                    else if (ADB_VERSION_MICRO_MAX != -1 && microVersion > ADB_VERSION_MICRO_MAX)
                        {
                        string message = string.Format("Required maximum version of adb: {0}.{1}.{2}. Current version is {0}.{1}.{3}",
                                        majorVersion, minorVersion, ADB_VERSION_MICRO_MAX, microVersion);
                        Log.LogAndDisplay(LogLevel.Error, ADB, message);
                        }
                    else
                        {
                        this.DoVersionCheck = true;
                        }
                    return true;
                    }
                }
            return false;
            }

        /// <summary>
        /// Starts the adb host side server.
        /// </summary>
        /// <returns>true if success</returns>
        private bool StartAdb()
            {
            if (string.IsNullOrEmpty(PathToAdbExe))
                {
                Log.e(ADB, "Cannot start adb when AndroidDebugBridge is created without the location of adb.");
                return false;
                }

            int status = -1;

            try
                {
                string command = "start-server";
                Log.d(DDMS, $"Launching '{PathToAdbExe} {command}' to ensure ADB is running.");
                ProcessStartInfo psi = new ProcessStartInfo(PathToAdbExe, command);
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

                using (Process proc = Process.Start(psi))
                    {
                    List<string> errorOutput = new List<string>();
                    List<string> stdOutput = new List<string>();
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

            Log.d(DDMS, "'adb start-server' succeeded");
            return true;
            }

        /// <summary>
        /// Stops the adb host side server.
        /// </summary>
        /// <returns>true if success</returns>
        private bool KillAdb()
            {
            if (string.IsNullOrEmpty(PathToAdbExe))
                {
                Log.e(ADB, "Cannot stop adb when AndroidDebugBridge is created without the location of adb.");
                return false;
                }
            int status = -1;

            try
                {
                string command = "kill-server";
                ProcessStartInfo psi = new ProcessStartInfo(PathToAdbExe, command);
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

                using (Process proc = Process.Start(psi))
                    {
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

        /// <summary>
        /// Get the stderr/stdout outputs of a process and return when the process is done.
        /// Both <b>must</b> be read or the process will block on windows.
        /// </summary>
        /// <param name="process">The process to get the ouput from</param>
        /// <param name="errorOutput">The array to store the stderr output. cannot be null.</param>
        /// <param name="stdOutput">The array to store the stdout output. cannot be null.</param>
        /// <param name="waitforReaders">if true, this will wait for the reader threads.</param>
        /// <returns>the process return code.</returns>
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
                                Log.e(ADB, line);
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
        #endregion
        }
    }

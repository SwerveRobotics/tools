using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Org.SwerveRobotics.Tools.ManagedADB.Exceptions;
using Org.SwerveRobotics.Tools.ManagedADB.IO;
using Org.SwerveRobotics.Tools.ManagedADB.Logs;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public enum DeviceState
        {
        Recovery,
        BootLoader,
        Offline,
        Online,
        Download,
        Unknown
        }

    public sealed class Device : IDevice
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public const string MNT_EXTERNAL_STORAGE = "EXTERNAL_STORAGE";
        public const string MNT_DATA             = "ANDROID_DATA";
        public const string MNT_ROOT             = "ANDROID_ROOT";

        public const string TEMP_DIRECTORY_FOR_INSTALL  = "/storage/sdcard0/tmp/";
        public const string PROP_BUILD_VERSION          = "ro.build.version.release";
        public const string PROP_BUILD_API_LEVEL        = "ro.build.version.sdk";
        public const string PROP_BUILD_CODENAME         = "ro.build.version.codename";
        public const string PROP_DEBUGGABLE             = "ro.debuggable";
        public const string FIRST_EMULATOR_SN           = "emulator-5554";

        /** @deprecated Use {@link #PROP_BUILD_API_LEVEL}. */
        [Obsolete("Use PROP_BUILD_API_LEVEL")] public const string PROP_BUILD_VERSION_NUMBER = PROP_BUILD_API_LEVEL;

        private const string RE_EMULATOR_SN             = @"emulator-(\d+)";
        private const string RE_IPADDR_SN               = "[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}:[0-9]{1,5}"; // A regular expression that matches an IP address

        private const string RE_DEVICELIST_INFO         = @"^([a-z0-9_-]+(?:\s?[\.a-z0-9_-]+)?(?:\:\d{1,})?)\s+(device|offline|unknown|bootloader|recovery|download)(?:\s+product:([\S]+)\s+model\:([\S]+)\s+device\:([\S]+))?$";
        
        private const string LOG_TAG                    = "Device";

        private const int BATTERY_TIMEOUT = 2*1000; //2 seconds
        private const int GETPROP_TIMEOUT = 2*1000; //2 seconds
        private const int INSTALL_TIMEOUT = 2*60*1000; // 2 minutes

        private string          avdName;
        private bool            canSU                = false;
        private BatteryInfo     lastBatteryInfo      = null;
        private DateTime        lastBatteryCheckTime = DateTime.MinValue;
        private string          usbSerialNumber      = null;

        public string                         SerialNumber { get; }
        public string                         USBSerialNumber { 
                                                    get { return this.GetProperty("ro.boot.serialno") ?? (this.SerialNumberIsUSB ? this.SerialNumber : this.usbSerialNumber); } 
                                                    set { this.usbSerialNumber = value; }
                                                    }
        public IPEndPoint                     Endpoint { get; private set; }
        public TransportType                  TransportType { get; private set; }
        public string                         Product { get; private set; }
        public string                         Model { get; private set; }
        public string                         DeviceProperty { get; private set; }
        public Dictionary<string, MountPoint> MountPoints { get; set; }
        public Dictionary<string, string>     Properties { get; }
        public Dictionary<string, string>     EnvironmentVariables { get; }
        public List<IClient>                  Clients { get; }
        public FileSystem                     FileSystem { get; }
        public BusyBox                        BusyBox { get; }
        public bool                           SerialNumberIsUSB         => !this.SerialNumberIsEmulator && !this.SerialNumberIsTCPIP;
        public bool                           SerialNumberIsEmulator    => this.SerialNumber.IsMatch(RE_EMULATOR_SN);
        public bool                           SerialNumberIsTCPIP       => this.SerialNumber.IsMatch(RE_IPADDR_SN);
        public string                         IpAddress                 => this.GetProperty("dhcp.wlan0.ipaddress");
        public bool                           WifiIsOn                  => this.GetProperty("init.svc.dhcpcd_wlan0")=="running";
        public DeviceState                    State             { get; internal set; }
        public bool                           IsOnline           => this.State == DeviceState.Online;
        public bool                           IsOffline          => this.State == DeviceState.Offline;
        public bool                           IsBootLoader       => this.State == DeviceState.BootLoader;
        public bool                           IsRecovery         => this.State == DeviceState.Recovery;
        public bool                           HasClients         => this.Clients.Count > 0;
        public PackageManager                 PackageManager     => new PackageManager(this);
        public FileListingService             FileListingService => new FileListingService(this);
        public RawImage                       Screenshot         => AdbHelper.Instance.GetFrameBuffer(AndroidDebugBridge.AdbServerSocketAddress, this);

        public event EventHandler<EventArgs>  StateChanged;
        public event EventHandler<EventArgs>  BuildInfoChanged;
        public event EventHandler<EventArgs>  ClientListChanged;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public Device(string serial, DeviceState state, string model, string product, string device)
            {
            this.SerialNumber            = serial;
            this.State                   = state;
            this.Model                   = model;
            this.Product                 = product;
            this.DeviceProperty          = device;

            this.MountPoints             = new Dictionary<string, MountPoint>();
            this.Properties              = new Dictionary<string, string>();
            this.EnvironmentVariables    = new Dictionary<string, string>();
            this.Clients                 = new List<IClient>();
            this.FileSystem              = new FileSystem(this);
            this.BusyBox                 = new BusyBox(this);

            RetrieveDeviceInfo();
            }

        private void RetrieveDeviceInfo()
            {
            RefreshMountPoints();
            RefreshEnvironmentVariables();
            RefreshProperties();
            }

        private static DeviceState GetStateFromString(string state)
            {
            string tstate = state;

            if (Util.equals(state, "device"))
                {
                tstate = "online";
                }

            if (Enum.IsDefined(typeof (DeviceState), tstate))
                {
                return (DeviceState) Enum.Parse(typeof (DeviceState), tstate, true);
                }
            else
                {
                foreach (FieldInfo fi in typeof (DeviceState).GetFields())
                    {
                    if (Util.equalsIgnoreCase(fi.Name, tstate))
                        {
                        return (DeviceState) fi.GetValue(null);
                        }
                    }
                }

            return DeviceState.Unknown;
            }


        public static Device CreateFromAdbData(string deviceData)
            {
            Regex re = new Regex(RE_DEVICELIST_INFO, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Match m = re.Match(deviceData);
            if (m.Success)
                {
                return new Device(m.Groups[1].Value, GetStateFromString(m.Groups[2].Value), m.Groups[4].Value, m.Groups[3].Value, m.Groups[5].Value);
                }
            else
                {
                throw new ArgumentException($"CreateFromAdbData: invalid data: '{deviceData}'");
                }
            }

        public bool CanBackup()
            {
            return this.FileSystem.Exists("/system/bin/bu");
            }

        public bool CanSU()
            {
            if (this.canSU)
                {
                return this.canSU;
                }

            try
                {
                // workitem: 16822 this now checks if permission was denied and accounts for that. The nulloutput receiver is fine
                // here because it doesn't need to send the output anywhere, the execute command can still handle the output with
                // the null output receiver. 
                this.ExecuteRootShellCommand("echo \\\"I can haz root\\\"", NullOutputReceiver.Instance);
                this.canSU = true;
                }
            catch (PermissionDeniedException)
                {
                this.canSU = false;
                }
            catch (FileNotFoundException)
                {
                this.canSU = false;
                }

            return this.canSU;
            }

        public string AvdName
            {
            get { return this.avdName; }
            set
                {
                if (!this.SerialNumberIsEmulator)
                    {
                    throw new ArgumentException("Cannot set the AVD name of the device is not an emulator");
                    }
                this.avdName = value;
                }
            }

        public string GetProperty(string name)
            {
            return GetProperty(new[] {name});
            }

        public string GetProperty(params string[] names)
            {
            foreach (string name in names)
                {
                if (this.Properties.ContainsKey(name))
                    {
                    return this.Properties[name];
                    }
                }

            return null;
            }


        public void RemountMountPoint(MountPoint mnt, bool readOnly)
            {
            string command = $"mount -o {(readOnly ? "ro" : "rw")},remount -t {mnt.FileSystem} {mnt.Block} {mnt.Name}";
            this.ExecuteShellCommand(command, NullOutputReceiver.Instance);
            RefreshMountPoints();
            }

        public void RemountMountPoint(string mountPoint, bool readOnly)
            {
            if (this.MountPoints.ContainsKey(mountPoint))
                {
                MountPoint mnt = this.MountPoints[mountPoint];
                RemountMountPoint(mnt, readOnly);
                }
            else
                {
                throw new IOException("Invalid mount point");
                }
            }

        public void RefreshMountPoints()
            {
            if (!this.IsOffline)
                {
                try
                    {
                    this.ExecuteShellCommand(MountPointReceiver.MOUNT_COMMAND, new MountPointReceiver(this));
                    }
                catch (AdbException)
                    {
                    }
                }
            }

        public void RefreshEnvironmentVariables()
            {
            if (!this.IsOffline)
                {
                try
                    {
                    this.ExecuteShellCommand(EnvironmentVariablesReceiver.ENV_COMMAND, new EnvironmentVariablesReceiver(this));
                    }
                catch (AdbException)
                    {
                    }
                }
            }

        public void RefreshProperties()
            {
            if (!this.IsOffline)
                {
                try
                    {
                    this.ExecuteShellCommand(GetPropReceiver.GETPROP_COMMAND, new GetPropReceiver(this));
                    }
                catch (AdbException aex)
                    {
                    Log.w(LOG_TAG, aex);
                    }
                }
            }

        public void Reboot(string into)
            {
            AdbHelper.Instance.Reboot(into, AndroidDebugBridge.AdbServerSocketAddress, this);
            }

        public void Reboot()
            {
            Reboot(string.Empty);
            }

        public BatteryInfo GetBatteryInfo()
            {
            return GetBatteryInfo(5*60*1000);
            }

        public BatteryInfo GetBatteryInfo(long freshness)
            {
            if (this.lastBatteryInfo != null && this.lastBatteryCheckTime > (DateTime.Now.AddMilliseconds(-freshness)))
                {
                return this.lastBatteryInfo;
                }
            BatteryReceiver receiver = new BatteryReceiver();
            ExecuteShellCommand("dumpsys battery", receiver, BATTERY_TIMEOUT);
            this.lastBatteryInfo = receiver.BatteryInfo;
            this.lastBatteryCheckTime = DateTime.Now;
            return this.lastBatteryInfo;
            }

        public SyncService SyncService
        // Returns null if the SyncService couldn't be created. This can happen if adb
        // refuse to open the connection because the {@link IDevice} is invalid (or got disconnected).
        // Throws if the connection with adb failed
            {
            get
                {
                SyncService syncService = new SyncService(AndroidDebugBridge.AdbServerSocketAddress, this);
                if (syncService.Open())
                    {
                    return syncService;
                    }

                return null;
                }
            }

        public void ExecuteShellCommand(string command, IShellOutputReceiver receiver)
            {
            ExecuteShellCommand(command, receiver, new object[] {});
            }

        public void ExecuteShellCommand(string command, IShellOutputReceiver receiver, int timeout)
            {
            ExecuteShellCommand(command, receiver, new object[] {});
            }

        public void ExecuteShellCommand(string command, IShellOutputReceiver receiver, params object[] commandArgs)
            {
            AdbHelper.Instance.ExecuteRemoteCommand(AndroidDebugBridge.AdbServerSocketAddress, string.Format(command, commandArgs), this, receiver);
            }

        public void ExecuteShellCommand(string command, IShellOutputReceiver receiver, int timeout, params object[] commandArgs)
            {
            AdbHelper.Instance.ExecuteRemoteCommand(AndroidDebugBridge.AdbServerSocketAddress, string.Format(command, commandArgs), this, receiver);
            }

        public void ExecuteRootShellCommand(string command, IShellOutputReceiver receiver, int timeout)
            {
            ExecuteRootShellCommand(command, receiver, timeout, new object[] {});
            }

        public void ExecuteRootShellCommand(string command, IShellOutputReceiver receiver)
            {
            ExecuteRootShellCommand(command, receiver, int.MaxValue);
            }

        public void ExecuteRootShellCommand(string command, IShellOutputReceiver receiver, params object[] commandArgs)
            {
            ExecuteRootShellCommand(command, receiver, int.MaxValue, commandArgs);
            }

        public void ExecuteRootShellCommand(string command, IShellOutputReceiver receiver, int timeout, params object[] commandArgs)
            {
            AdbHelper.Instance.ExecuteRemoteRootCommand(AndroidDebugBridge.AdbServerSocketAddress, string.Format(command, commandArgs), this, receiver, timeout);
            }

        public void RunEventLogService(LogReceiver receiver)
            {
            AdbHelper.Instance.RunEventLogService(AndroidDebugBridge.AdbServerSocketAddress, this, receiver);
            }

        public void RunLogService(string logname, LogReceiver receiver)
            {
            AdbHelper.Instance.RunLogService(AndroidDebugBridge.AdbServerSocketAddress, this, logname, receiver);
            }

        public bool CreateForward(int localPort, int remotePort)
            {
            try
                {
                return AdbHelper.Instance.CreateForward(AndroidDebugBridge.AdbServerSocketAddress, this, localPort, remotePort);
                }
            catch (IOException e)
                {
                Log.w("ddms", e);
                return false;
                }
            }

        public bool RemoveForward(int localPort)
            {
            try
                {
                return AdbHelper.Instance.RemoveForward(AndroidDebugBridge.AdbServerSocketAddress, this, localPort);
                }
            catch (IOException e)
                {
                Log.w("ddms", e);
                return false;
                }
            }

        /*
		public String GetClientName ( int pid ) {
			lock ( ClientList ) {
				foreach ( Client c in ClientList ) {
					if ( c.ClientData ( ).Pid == pid ) {
						return c.ClientData.ClientDescription;
					}
				}
			}

			return null;
		}

		DeviceMonitor Monitor { get; private set; }

		void AddClient ( Client client ) {
			lock ( ClientList ) {
				ClientList.Add ( client );
			}
		}

		List<Client> ClientList { get; private set; }

		bool HasClient ( int pid ) {
			lock ( ClientList ) {
				foreach ( Client client in ClientList ) {
					if ( client.ClientData.Pid == pid ) {
						return true;
					}
				}
			}

			return false;
		}

		void ClearClientList ( ) {
			lock ( ClientList ) {
				ClientList.Clear ( );
			}
		}
		
		SocketChannel ClientMonitoringSocket { get; set; }

		void RemoveClient ( Client client, bool notify ) {
			Monitor.AddPortToAvailableList ( client.DebuggerListenPort );
			lock ( ClientList ) {
				ClientList.Remove ( client );
			}
			if ( notify ) {
				Monitor.Server.DeviceChanged ( this, CHANGE_CLIENT_LIST );
			}
		}

		void Update ( int changeMask ) {
			Monitor.Server.DeviceChanged ( this, changeMask );
		}

		void Update ( Client client, int changeMask ) {
			Monitor.Server.ClientChanged ( client, changeMask );
		}
        */

        public void InstallPackage(string packageFilePath, bool reinstall)
        // This is a helper method that combines the syncPackageToDevice, installRemotePackage, and removePackage steps
            {
            string remoteFilePath = SyncPackageToDevice(packageFilePath);
            InstallRemotePackage(remoteFilePath, reinstall);
            RemoveRemotePackage(remoteFilePath);
            }

        public string SyncPackageToDevice(string localFilePath)
        // Throws if fatal error occurred when pushing file
            {
            try
                {
                string packageFileName = Path.GetFileName(localFilePath);
                // only root has access to /data/local/tmp/... not sure how adb does it then...
                // workitem: 16823
                // workitem: 19711
                string remoteFilePath = LinuxPath.Combine(TEMP_DIRECTORY_FOR_INSTALL, packageFileName);

                Util.ConsoleTraceError($"Uploading {packageFileName} onto device '{this.SerialNumber}'");
                Log.d(packageFileName, $"Uploading {packageFileName} onto device '{this.SerialNumber}'");

                SyncService sync = SyncService;
                if (sync != null)
                    {
                    string message = $"Uploading file onto device '{this.SerialNumber}'";
                    Log.d(LOG_TAG, message);
                    SyncResult result = sync.PushFile(localFilePath, remoteFilePath, SyncService.NullProgressMonitor);

                    if (result.Code != ErrorCodeHelper.RESULT_OK)
                        {
                        throw new IOException($"Unable to upload file: {result.Message}");
                        }
                    }
                else
                    {
                    throw new IOException("Unable to open sync connection!");
                    }
                return remoteFilePath;
                }
            catch (IOException e)
                {
                Log.e(LOG_TAG, $"Unable to open sync connection! reason: {e.Message}");
                throw;
                }
            }

        public void InstallRemotePackage(string remoteFilePath, bool reinstall)
            {
            InstallReceiver receiver = new InstallReceiver();
            FileEntry entry = this.FileListingService.FindFileEntry(remoteFilePath);
            string cmd = string.Format("pm install {1}{0}", entry.FullEscapedPath, reinstall ? "-r " : string.Empty);
            ExecuteShellCommand(cmd, receiver);

            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
                {
                throw new PackageInstallationException(receiver.ErrorMessage);
                }
            }


        public void RemoveRemotePackage(string remoteFilePath)
            {
            // now we delete the app we sync'ed
            try
                {
                ExecuteShellCommand("rm " + remoteFilePath, NullOutputReceiver.Instance);
                }
            catch (IOException e)
                {
                Log.e(LOG_TAG, $"Failed to delete temporary package: {e.Message}");
                throw e;
                }
            }

        public void UninstallPackage(string packageName)
            {
            InstallReceiver receiver = new InstallReceiver();
            ExecuteShellCommand($"pm uninstall {packageName}", receiver);
            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
                {
                throw new PackageInstallationException(receiver.ErrorMessage);
                }
            }

        internal void OnStateChanged(EventArgs e)
            {
            this.StateChanged?.Invoke(this, e);
            }

        internal void OnBuildInfoChanged(EventArgs e)
            {
            this.BuildInfoChanged?.Invoke(this, e);
            }

        internal void OnClientListChanged(EventArgs e)
            {
            this.ClientListChanged?.Invoke(this, e);
            }
        }
    }
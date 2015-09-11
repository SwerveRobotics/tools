using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.IO;
using Managed.Adb;
using static Org.SwerveRobotics.BlueBotBug.Service.WIN32;

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    public interface ITracer
        { 
        void Trace(string format, params object[] args);
        void Log(string format, params object[] args);
        }

    public unsafe class DeviceEventArgs : EventArgs
        {
        public DEV_BROADCAST_HDR* pHeader;
        }
    public unsafe class DeviceEventArgsCancel : DeviceEventArgs
        {
        public bool Cancel = false;
        }

    public interface IDeviceEvents
        {
        event EventHandler<DeviceEventArgs>       DeviceArrived;
        event EventHandler<DeviceEventArgsCancel> DeviceQueryRemove;
        event EventHandler<DeviceEventArgs>       DeviceQueryRemoveFailed;
        event EventHandler<DeviceEventArgs>       DeviceRemovePending;
        event EventHandler<DeviceEventArgs>       DeviceRemoveComplete;
        event EventHandler<DeviceEventArgs>       DeviceTypeSpecific;
        event EventHandler<DeviceEventArgs>       DeviceCustomEvent;
        event EventHandler<DeviceEventArgs>       DeviceUserDefined;
        event EventHandler<CancelEventArgs>       DeviceQueryChangeConfig;
        event EventHandler<EventArgs>             DeviceConfigChanged;
        event EventHandler<EventArgs>             DeviceConfigChangeCancelled;
        event EventHandler<EventArgs>             DeviceDevNodesChanged;
        }

    public class USBMonitor : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        IDeviceEvents       eventRaiser = null;
        ITracer             tracer      = null;
        bool                started     = false;
        IntPtr              notificationHandle;
        bool                notificationHandleIsService;

        readonly object     theLock = new object();
        List<Guid>          deviceInterfacesOfInterest = null;
        List<IntPtr>        deviceNotificationHandles = null;
        AndroidDebugBridge  bridge = null;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public USBMonitor(IDeviceEvents eventRaiser, ITracer tracer, IntPtr notificationHandle, bool notificationHandleIsService)
            {
            this.eventRaiser = eventRaiser;
            this.tracer = tracer;
            this.notificationHandle = notificationHandle;
            this.notificationHandleIsService = notificationHandleIsService;
            this.Initialize();
            }

        ~USBMonitor()
            {
            this.Dispose(false);
            }

        void IDisposable.Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        void Initialize()
            {
            lock (theLock)
                {
                this.deviceInterfacesOfInterest = new List<Guid>();
                this.deviceNotificationHandles = new List<IntPtr>();
                }
            this.started = false;
            }

        public virtual void Dispose(bool fromUserCode)
            {
            if (fromUserCode)
                {
                // Called from user's code. Can / should cleanup managed objects
                }

            // Called from finalizers (and user code). Avoid referencing other objects
            this.ReleaseDeviceNotificationHandles();
            }

        //-----------------------------------------------------------------------------------------
        // Device notification management
        //-----------------------------------------------------------------------------------------

        public void AddDeviceInterfaceOfInterest(Guid guid)
            {
            lock (theLock)
                {
                this.deviceInterfacesOfInterest.Add(guid);
                }

            if (this.started)
                {
                GetUSBDeviceNotificationsFor(guid);
                }
            }

        void GetUSBDeviceNotificationsFor(Guid guidDevInterface)
            {
            lock (theLock)
                {
                DEV_BROADCAST_DEVICEINTERFACE_MANAGED filter = new DEV_BROADCAST_DEVICEINTERFACE_MANAGED();
                filter.Initialize(guidDevInterface);

                IntPtr hDeviceNotify = RegisterDeviceNotification(this.notificationHandle, filter, this.notificationHandleIsService ? DEVICE_NOTIFY_SERVICE_HANDLE : DEVICE_NOTIFY_WINDOW_HANDLE);
                ThrowIfFail(hDeviceNotify);

                this.deviceNotificationHandles.Add(hDeviceNotify);
                }
            }

        void ReleaseDeviceNotificationHandles()
            {
            lock (theLock)
                {
                foreach (IntPtr hDeviceNotify in this.deviceNotificationHandles)
                    {
                    UnregisterDeviceNotification(hDeviceNotify);
                    }
                this.deviceNotificationHandles = new List<IntPtr>();
                }
            }

        public void Start()
            {
            try
                {
                string path = GetAdbPath();
                this.bridge = AndroidDebugBridge.OpenBridge(path, true);
                this.bridge.DeviceConnected += (object sender, Managed.Adb.DeviceEventArgs e) =>
                    {
                    EnsureAdbDevicesAreOnTCPIP("ADB device connected notification");
                    };

                this.eventRaiser.DeviceArrived        += OnDeviceArrived;
                this.eventRaiser.DeviceRemoveComplete += OnDeviceRemoveComplete;

                foreach (Guid guid in this.deviceInterfacesOfInterest)
                    {
                    GetUSBDeviceNotificationsFor(guid);
                    }

                EnsureAdbDevicesAreOnTCPIP("start");
                
                this.started = true;
                }
            catch (Exception)
                {
                Stop();
                throw;
                }

            }

        public void Stop()
            {
            this.started = false;

            this.ReleaseDeviceNotificationHandles();

            this.eventRaiser.DeviceArrived        -= OnDeviceArrived;
            this.eventRaiser.DeviceRemoveComplete -= OnDeviceRemoveComplete;

            this.bridge?.StopMonitoring();
            this.bridge = null;
            }

        //-----------------------------------------------------------------------------------------
        // ADB
        //-----------------------------------------------------------------------------------------

        string GetAdbPath()
        // Return the path to the ADB.EXE executable that we are to use
        // TODO: This should look for the Android SDK version first, and use what we have here only as a last resort.
        // HKEY_LOCAL_MACHINE\SOFTWARE\Android Studio@SdkPath
            {
            string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string result = Path.Combine(dir, "adb.exe");
            return result;
            }

        object ensureAdbDevicesAreOnTCPIPLock = new object();

        void EnsureAdbDevicesAreOnTCPIP(string reason)
        // Iterate over all the extant Android devices (that ADB knows about) and make sure that each
        // one of them is listening on TCPIP. This method is idempotent, so you can call it as often
        // and as frequently as you like.
            {
            // We synchronize for paranoid reasons: we're not SURE we can be be called on
            // a whole range of threads, possibly simultaneously, but why take the chance?
            lock (ensureAdbDevicesAreOnTCPIPLock)
                {
                this.tracer.Trace("------");
                this.tracer.Trace($"EnsureAdbDevicesAreOnTCPIP({reason})");

                // Keep track of which devices are already listening as we want them to be
                HashSet<string> ipAddressesAlreadyListening = new HashSet<string>(); 

                // Get ourselves the list of extant devices
                List<Device> devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);

                // A regular expression that matches an IP address
                const string ipPattern = "[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}:[0-9]{1,5}";

                // Iterate over that list, finding out who is already listening
                this.tracer.Trace($"   current devices:");
                foreach (Device device in devices)
                    {
                    // If the device doesn't have an IP address, we can't do anything
                    if (device.IpAddress() == null)
                        continue;

                    this.tracer.Trace($"      serialNumber:{device.SerialNumber}");

                    // Is this guy already listening as we want him to?
                    if (device.SerialNumber.IsMatch(ipPattern))
                        ipAddressesAlreadyListening.Add(device.IpAddress());
                    }

                // Iterate again over that list, ensuring that any that are not listening start to do so
                foreach (Device device in devices)
                    {
                    string ipAddress = device.IpAddress();

                    // If the device doesn't have an IP address, we can't do anything
                    if (ipAddress == null)
                        continue;

                    // If he's already listening, we're good
                    if (ipAddressesAlreadyListening.Contains(ipAddress))
                        continue;

                    // Restart the device listening on a port of interest
                    this.tracer.Trace($"   restarting {ipAddress} in TCPIP mode");
                    int portNumber = 5555;
                    AdbHelper.Instance.TcpIp(portNumber, AndroidDebugBridge.SocketAddress, device);
                    
                    // Give it a chance to restart. The actual time used here is a total guess, but
                    // it does seem to work. Mostly (?).
                    Thread.Sleep(1000);

                    // Connect to the TCPIP version of that device
                    this.tracer.Trace($"   connecting to restarted {ipAddress} device");
                    AdbHelper.Instance.Connect(ipAddress, portNumber, AndroidDebugBridge.SocketAddress);

                    this.tracer.Trace($"   connected to {ipAddress}");
                    }
                }
            }

        //-----------------------------------------------------------------------------------------
        // Win32 Events
        //-----------------------------------------------------------------------------------------

        unsafe void OnDeviceArrived(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                Trace("added", pintf);
                EnsureAdbDevicesAreOnTCPIP("OnDeviceArrived");
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                Trace("removed", pintf);
                }
            }

        unsafe void Trace(string message, DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            this.tracer.Trace("{0}: ", message);
            this.tracer.Trace("    devicePath={0}",     pintf->dbcc_name);
            this.tracer.Trace("    guid={0}",           pintf->dbcc_classguid);
            }
        }
    }


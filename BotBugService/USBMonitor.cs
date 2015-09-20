using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net;
using Org.SwerveRobotics.Tools.ManagedADB;
using Org.SwerveRobotics.Tools.Util;
using static Org.SwerveRobotics.Tools.BotBug.Service.WIN32;
using Org.SwerveRobotics.Tools.BotBug.Service.Properties;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    public class USBMonitor : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        const int           devicePort  = 5555;        // the port number we always use to connect to devices

        bool                disposed    = false;
        IDeviceEvents       eventRaiser = null;
        ITracer             tracer      = null;
        bool                started     = false;
        IntPtr              notificationHandle;
        bool                notificationHandleIsService;

        readonly object         deviceConnectionLock            = new object();
        readonly object         deviceListLock                  = new object();

        List<Guid>              deviceInterfacesOfInterest      = null;
        List<IntPtr>            deviceNotificationHandles       = null;
        AndroidDebugBridge      bridge                          = null;
        SharedMemTaggedBlobQueue bugbotMessageQueue             = null;
        SharedMemTaggedBlobQueue bugBotCommandQueue             = null;
        HandshakeThreadStarter   commandQueueStarter            = null;
        Device                  lastTPCIPDevice                 = null;
        IPEndPoint              lastTCPIPEndPoint               = null;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public USBMonitor(IDeviceEvents eventRaiser, ITracer tracer, IntPtr notificationHandle, bool notificationHandleIsService)
            {
            this.eventRaiser                 = eventRaiser;
            this.tracer                      = tracer;
            this.notificationHandle          = notificationHandle;
            this.notificationHandleIsService = notificationHandleIsService;

            this.bugbotMessageQueue = new SharedMemTaggedBlobQueue(true, TaggedBlob.BugBotMessageQueueUniquifier);
            this.bugbotMessageQueue.InitializeIfNecessary();
            this.bugBotCommandQueue = new SharedMemTaggedBlobQueue(true, TaggedBlob.BugBotCommandQueueUniquifier);
            this.bugBotCommandQueue.InitializeIfNecessary();

            this.commandQueueStarter = new HandshakeThreadStarter("Command Q Listener", CommandQueueListenerThread);

            NotifyNoRememberedConnections();
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotMessage, Resources.StartingMessage);

            this.deviceInterfacesOfInterest = new List<Guid>();
            this.deviceNotificationHandles  = new List<IntPtr>();

            this.started = false;
            }

        ~USBMonitor()
            {
            this.Dispose(false);
            }

        public void Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected virtual void Dispose(bool notFromFinalizer)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (notFromFinalizer)
                    {
                    this.bugbotMessageQueue?.Write(TaggedBlob.TagBugBotMessage, Resources.StoppingMessage);
                    }
                this.bugbotMessageQueue?.Dispose();     this.bugbotMessageQueue = null;
                this.bugBotCommandQueue?.Dispose();     this.bugBotCommandQueue = null;
                this.commandQueueStarter?.Dispose();    this.commandQueueStarter = null;
                this.ReleaseDeviceNotificationHandles();
                }
            }

        //-----------------------------------------------------------------------------------------
        // Device notification management
        //-----------------------------------------------------------------------------------------

        public void AddDeviceInterfaceOfInterest(Guid guid)
            {
            lock (this.deviceListLock)
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
            lock (this.deviceListLock)
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
            lock (this.deviceListLock)
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
                Log.ThresholdLevel = LogLevel.Debug;

                this.bridge = AndroidDebugBridge.Create();
                this.bridge.DeviceConnected += (sender, e) =>
                    {
                    this.IgnoreADBExceptionsDuring(() => EnsureUSBConnectedDevicesAreOnTCPIP("ADB device connected notification"));
                    };
                this.bridge.ServerStartedOrReconnected += (sender, e) =>
                    {
                    this.IgnoreADBExceptionsDuring(() => 
                        {
                        lock (this.deviceConnectionLock)
                            {
                            // Ensure any USB-connected devices are on TCPIP
                            bool anyDevices = EnsureUSBConnectedDevicesAreOnTCPIP("ADB server started notification");

                            // If the server isn't in fact connected to ANYONE (maybe the server stopped
                            // while no USB device was connected; closing Android Studio stops the server)
                            // then try to connect it to the last TCPIP device we used
                            if (!anyDevices)
                                {
                                ReconnectToLastTCPIPDevice();
                                }
                            }
                        });
                    };

                this.eventRaiser.DeviceArrived        += OnDeviceArrived;
                this.eventRaiser.DeviceRemoveComplete += OnDeviceRemoveComplete;

                foreach (Guid guid in this.deviceInterfacesOfInterest)
                    {
                    GetUSBDeviceNotificationsFor(guid);
                    }

                this.IgnoreADBExceptionsDuring(() => EnsureUSBConnectedDevicesAreOnTCPIP("BotBug start"));

                this.StartCommandQueueListener();

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

            this.StopCommandQueueListener();
            this.ReleaseDeviceNotificationHandles();

            this.eventRaiser.DeviceArrived        -= OnDeviceArrived;
            this.eventRaiser.DeviceRemoveComplete -= OnDeviceRemoveComplete;

            this.bridge?.StopTracking();
            this.bridge = null;
            }

        //-----------------------------------------------------------------------------------------
        // ADB
        //-----------------------------------------------------------------------------------------
        
        void IgnoreADBExceptionsDuring(Action action)
            {
            try
                {
                action.Invoke();
                }
            catch (Exception ex) when (IsADBIOException(ex))
                {
                // Talking to ADB can throw exceptions, depending on how ADB server comes and goes, etc.
                // We ignore those in the expectation that Managed.ADB will repair those and make us whole again
                this.tracer.Trace($"ADB exception ignored: {ex}");
                }
            }

        bool IsADBIOException(Exception e)
        // We don't have a full enumeration of all exceptions that ADB may ACTUALLY throw, 
        // so we are conservative
            {
            return true;
            }

        void RememberLastTCPIPDevice(Device device)
            {
            lock (this.deviceConnectionLock)
                {
                this.lastTPCIPDevice    = device;
                this.lastTCPIPEndPoint  = new IPEndPoint(IPAddress.Parse(device.IpAddress), devicePort);
                NotifyRememberedConnection();
                }
            }

        void ForgetLastTCPIPDevice()
            {
            lock (this.deviceConnectionLock)
                {
                if (this.lastTCPIPEndPoint != null)
                    {
                    this.lastTCPIPEndPoint = null;
                    NotifyNoRememberedConnections();
                    }
                }            
            }
       
        bool EnsureUSBConnectedDevicesAreOnTCPIP(string reason)
        // Iterate over all the extant Android devices (that ADB knows about) and make sure that each
        // one of them is listening on TCPIP. This method is idempotent, so you can call it as often
        // and as frequently as you like. Answer as to whether there were any devices known about by
        // the ADB server.
            {
            bool result = false;

            // We synchronize for paranoid reasons: we're not SURE we can be be called on
            // a whole range of threads, possibly simultaneously, but why take the chance?
            lock (this.deviceConnectionLock)
                {
                this.tracer.Trace($"v-----EnsureAdbDevicesAreOnTCPIP({reason})-----v");

                // Keep track of which devices are already listening as we want them to be
                HashSet<string> ipAddressesAlreadyConnectedToAdbServer = new HashSet<string>(); 
                Dictionary<string,List<Device>> mpIpAddressDevice = new Dictionary<string, List<Device>>();

                // Get ourselves the list of extant devices
                List<Device> devicesConnectedToAdbServer = AdbHelper.Instance.GetDevices(AndroidDebugBridge.AdbServerSocketAddress);

                // Iterate over that list, finding out who is already listening
                foreach (Device device in devicesConnectedToAdbServer)
                    {
                    // Yes, the server knows about devices
                    result = true;
                    this.tracer.Trace($"   serialNumber:{device.SerialNumber} ipAddress:{device.IpAddress} wifi:{(device.WifiIsOn?"on":"off")}");

                    // If the device doesn't have an IP address, we can't do anything
                    if (device.IpAddress != null)
                        {
                        // Keep track of multiple endpoints of the same device
                        List<Device> devices;
                        if (mpIpAddressDevice.TryGetValue(device.IpAddress, out devices))
                            devices.Add(device);
                        else
                            mpIpAddressDevice[device.IpAddress] = new List<Device> { device };

                        // Is this guy already listening as we want him to?
                        if (device.SerialNumberIsTCPIP)
                            {
                            ipAddressesAlreadyConnectedToAdbServer.Add(device.IpAddress);
                            }
                        }
                    }

                // Crosspolinate USB serial numbers. This might be useful for reconnecting to the last device, for example.
                // That said, most (all?) Devices can report their own serial numbers now, so this is less useful.
                foreach (KeyValuePair<string, List<Device>> pair in mpIpAddressDevice)
                    {
                    string serialUSB = null;
                    foreach (Device device in pair.Value)
                        {
                        if (device.USBSerialNumber != null)
                            {
                            serialUSB = device.USBSerialNumber;
                            break;
                            }
                        }
                    foreach (Device device in pair.Value)
                        {
                        device.USBSerialNumber = serialUSB;
                        }
                    }

                // Iterate again over that list, ensuring that any that are not listening start to do so
                Device potentialLastDevice = null;
                bool connected = false;
                foreach (Device device in devicesConnectedToAdbServer)
                    {
                    // Connect him if we can
                    if (device.IpAddress == null || !device.WifiIsOn)
                        {
                        // The device doesn't have an IP address or wifi is off, Adb server won't be able to connect
                        NotifyNotOnNetwork(device);
                        }
                    else if (ipAddressesAlreadyConnectedToAdbServer.Contains(device.IpAddress))
                        {
                        // He is already connected, but we might want to rember him as 'last connected'
                        potentialLastDevice = potentialLastDevice ?? device;
                        }
                    else
                        {
                        // He's not already connected, connect him
                        if (TcpipAndConnect(device))
                            connected = true;
                        }
                    }
                if (!connected && potentialLastDevice != null)
                    {
                    RememberLastTCPIPDevice(potentialLastDevice);
                    }

               this.tracer.Trace($"^-----EnsureAdbDevicesAreOnTCPIP({reason})-----^"); 
               }

            return result;
            }

        bool TcpipAndConnect(Device device)
            {
            bool result = false;
            string ipAddress = device.IpAddress;

            // Restart the device listening on a port of interest. We don't know if he got there,
            // as we get no response from the command issued.
            this.tracer.Trace($"   restarting {device.SerialNumber} in TCPIP mode at {ipAddress}");
            AdbHelper.Instance.TcpIp(AndroidDebugBridge.AdbServerSocketAddress, device, devicePort);
                    
            // Give it a chance to restart. The actual time used here is a total guess, but
            // it does seem to work. Mostly (?).
            Thread.Sleep(1000);

            // Connect to the TCPIP version of that device
            this.tracer.Trace($"   connecting to restarted {ipAddress} device");
            if (AdbHelper.Instance.Connect(AndroidDebugBridge.AdbServerSocketAddress, ipAddress, devicePort))
                {
                NotifyConnected(Resources.NotifyConnected, device, ipAddress, devicePort);

                // Remember to whom we last connected for later ADB Server restarts
                RememberLastTCPIPDevice(device);
                result = true;
                }
            else
                {
                this.tracer.Trace($"   failed to connect to {ipAddress}:{devicePort}");
                NotifyConnected(Resources.NotifyConnectedFail, device, ipAddress, devicePort);
                }

            return result;
            }

        void ReconnectToLastTCPIPDevice()
            {
            lock (this.deviceConnectionLock)
                {
                if (this.lastTCPIPEndPoint != null)
                    {
                    string ipAddress = this.lastTCPIPEndPoint.Address.ToString();
                    int portNumber = this.lastTCPIPEndPoint.Port;

                    this.tracer.Trace($"   reconnecting to {this.lastTPCIPDevice.USBSerialNumber} on {lastTCPIPEndPoint}");
                    if (AdbHelper.Instance.Connect(AndroidDebugBridge.AdbServerSocketAddress, ipAddress, portNumber))
                        NotifyReconnected(Resources.NotifyReconnected, this.lastTPCIPDevice, ipAddress, portNumber);
                    else
                        NotifyReconnected(Resources.NotifyReconnectedFail, this.lastTPCIPDevice, ipAddress, portNumber);
                    }
                }
            }

        //-----------------------------------------------------------------------------------------
        // Communicating with user mode
        //-----------------------------------------------------------------------------------------

        void NotifyRememberedConnection()
            {
            string message = string.Format(Resources.LastConnectedToMessage, this.lastTCPIPEndPoint);
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotStatus, message, 100);
            }

        void NotifyNoRememberedConnections()
            {
            string message = string.Format(Resources.NoLastConnectedToMessage);
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotStatus, message, 100);
            }

        void NotifyNotOnNetwork(Device device)
            {
            string message = string.Format(Resources.NotifyNotOnNetwork, device.USBSerialNumber??device.SerialNumber);
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotMessage, message, 100);
            }

        void NotifyConnected(string format, Device device, string ipAddress, int portNumber)
            {
            string message = string.Format(format, device.USBSerialNumber??device.SerialNumber, ipAddress, portNumber);
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotMessage, message, 100);
            }

        void NotifyReconnected(string format, Device device, string ipAddress, int portNumber)
            {
            string message = string.Format(format, device.USBSerialNumber??device.SerialNumber, ipAddress, portNumber);
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotMessage, message, 100);
            }

        void StartCommandQueueListener()
            {
            this.commandQueueStarter.Start();
            }

        void StopCommandQueueListener()
            {
            this.commandQueueStarter.Stop();
            }

        void CommandQueueListenerThread(HandshakeThreadStarter starter)
            {
            starter.DoHandshake();

            while (!starter.StopRequested)
                {
                List<TaggedBlob> blobs = this.bugBotCommandQueue.Read();
                foreach (TaggedBlob blob in blobs)
                    {
                    switch (blob.Tag)
                        {
                    case TaggedBlob.TagForgetLastConnection:
                        this.tracer.Trace($"TagForgetLastConnection command received");
                        ForgetLastTCPIPDevice();
                        break;
                        }
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
                EnsureUSBConnectedDevicesAreOnTCPIP("OnDeviceArrived");
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

    //===================================================================================================================
    // Utilities
    //===================================================================================================================
    // 
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

    }


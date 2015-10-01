//
// USBMonitor.cs
//
// Monitors devices coming and going over USB, both with system notifications and 
// with the help of ADB.
//
// Probably can be integrated into BotBugService itself.

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Org.SwerveRobotics.Tools.ManagedADB;
using Org.SwerveRobotics.Tools.Util;
using Org.SwerveRobotics.Tools.BotBug.Service.Properties;
using static Org.SwerveRobotics.Tools.BotBug.Service.WIN32;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    public class USBMonitor : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        const int                adbdPort  = 5555;        // the port number we always use to connect to devices

        bool                     disposed    = false;
        IDeviceEvents            eventRaiser = null;
        ITracer                  tracer      = null;
        bool                     started     = false;
        IntPtr                   notificationHandle;
        bool                     notificationHandleIsService;

        readonly object          deviceConnectionLock           = new object();
        readonly object          deviceListLock                 = new object();

        List<Guid>               deviceInterfacesOfInterest     = null;
        List<IntPtr>             deviceNotificationHandles      = null;
        AndroidDebugBridge       bridge                         = null;
        SharedMemTaggedBlobQueue bugbotMessageQueue             = null;
        SharedMemTaggedBlobQueue bugBotCommandQueue             = null;
        HandshakeThreadStarter   commandQueueStarter            = null;

        AndroidDeviceDatabase    androidDeviceDatabase          = null;
        AndroidDevice            lastTPCIPConnected             = null;
        AndroidDevice            reconnectionToVerify           = null;
        bool                     armed                          = true;
        readonly int             msPingTimeout                  = 500;

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

            this.androidDeviceDatabase = new AndroidDeviceDatabase();

            UpdateStatusNoRememberedConnections();
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

        // Add the indicated interface GUID as one of the interfaces for which we reeive OS notifications
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

        // Ask the OS to give us notifications for USB devices with the indicated interface
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
        
        // Undo the work of GetUSBDeviceNotificationsFor
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

        // Start the monitor running
        public void Start()
            {
            try
                {
                Log.ThresholdLevel = LogLevel.Debug;

                this.bridge = AndroidDebugBridge.Create();
                this.bridge.DeviceConnected += (sender, e) =>
                    {
                    this.IgnoreADBExceptionsDuring(() => EnsureUSBConnectedDevicesAreOnTCPIP("ADB device connected"));
                    };
                this.bridge.ServerStartedOrReconnected += (sender, e) =>
                    {
                    this.IgnoreADBExceptionsDuring(() => 
                        {
                        lock (this.deviceConnectionLock)
                            {
                            // Ensure any USB-connected devices are on TCPIP
                            bool? anyDevices = EnsureUSBConnectedDevicesAreOnTCPIP("ADB server started");

                            // If the server isn't in fact connected to ANYONE (maybe the server stopped
                            // while no USB device was connected; closing Android Studio stops the server)
                            // then try to connect it to the last TCPIP device we used
                            if (anyDevices.HasValue && !anyDevices.Value)
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

        // Stop the monitor running. This is idempotent
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
        // ADB integration
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
        // so we are conservative and just say that all exceptions are potentially from that communication
            {
            return true;
            }

        void RememberLastTCPIPDevice(AndroidDevice device)
            {
            lock (this.deviceConnectionLock)
                {
                this.lastTPCIPConnected = device;
                UpdateTrayStatus();
                }
            }

        void ForgetLastTCPIPDevice()
            {
            lock (this.deviceConnectionLock)
                {
                if (this.lastTPCIPConnected != null)
                    {
                    this.lastTPCIPConnected = null;
                    UpdateTrayStatus();
                    }
                }            
            }

        void UpdateTrayStatus()
            {
            lock (this.deviceConnectionLock)
                {
                if (this.lastTPCIPConnected == null)
                    UpdateStatusNoRememberedConnections();
                else
                    UpdateStatusRememberedConnection();
                }
            }
       
        bool? EnsureUSBConnectedDevicesAreOnTCPIP(string reason)
        // Iterate over all the extant Android devices (that ADB knows about) and make sure that each
        // one of them is listening on TCPIP. This method is idempotent, so you can call it as often
        // and as frequently as you like. Answer as to whether there were any devices known about by
        // the ADB server.
            {
            // Don't actually do anything if the user has asked us not to
            if (!this.armed)
                return null;

            bool serverKnowsAboutAnyDevices = false;

            // We synchronize for paranoid reasons: we're not SURE we can be be called on
            // a whole range of threads, possibly simultaneously, but why take the chance?
            lock (this.deviceConnectionLock)
                {
                this.tracer.Trace($"v-----EnsureAdbDevicesAreOnTCPIP({reason})-----v");

                // Get ourselves the list of devices that ADB currently knows about
                List<Device> devicesConnectedToAdbServer = AdbHelper.Instance.GetDevices(AndroidDebugBridge.AdbServerSocketAddress);
                this.androidDeviceDatabase.UpdateFromDevicesConnectedToAdbServer(devicesConnectedToAdbServer);

                // Do a bit of tracing
                foreach (Device device in devicesConnectedToAdbServer)
                    {
                    this.tracer.Trace($"   usb:{device.USBSerialNumber} ipAddress:{device.WlanIpAddress} wifi:{(device.WlanIsRunning?"on":"off")} serial:{device.SerialNumber}");
                    }
                this.tracer.Trace($"---------------------------------");
                foreach (AndroidDevice device in this.androidDeviceDatabase.ConnectedDevices)
                    {
                    this.tracer.Trace($"   usb:{device.USBSerialNumber} ipAddress:{device.WlanIpAddress} wifi:{(device.WlanIsRunning?"on":"off")} connected:{device.IsADBConnectedOnTcpip}");
                    }

                // Iterate over what's currently connected to ADB
                // 
                AndroidDevice potentialLastDevice = null;
                bool          connectedAny        = false;
                foreach (AndroidDevice device in this.androidDeviceDatabase.ConnectedDevices)
                    {
                    // Yes, the server knows about some devices
                    serverKnowsAboutAnyDevices = true;

                    if (device.IsADBConnectedOnTcpip)
                        {
                        // ADB has a TCPIP connection for him
                        // 
                        potentialLastDevice = potentialLastDevice ?? device;

                        if (this.reconnectionToVerify != null)
                            {
                            // Is this the address we reconnected on?
                            if (this.reconnectionToVerify.WlanIpAddress == device.WlanIpAddress)
                                {
                                // Did we reconnect to the same device?
                                if (this.reconnectionToVerify.USBSerialNumber == device.USBSerialNumber)
                                    {
                                    // All is well
                                    this.tracer.Trace($"   verify reconnected: all good: {this.reconnectionToVerify.WlanIpAddress} is still {device.USBSerialNumber}");
                                    }
                                else
                                    {
                                    // We reconnected to him, but he's the wrong guy. Disconnect.
                                    this.tracer.Trace($"   verify reconnected: fail: {this.reconnectionToVerify.WlanIpAddress}: got: {device.USBSerialNumber} expected: {this.reconnectionToVerify.USBSerialNumber}; disconnecting");
                                    AdbHelper.Instance.Disconnect(AndroidDebugBridge.AdbServerSocketAddress, device.WlanIpAddress, adbdPort);
                                    NotifyReconnected(Resources.NotifyReconnectedFail, device.UserIdentifier, device.WlanIpAddress);
                                    
                                    // If we're *still* porentially reconnecting to that same guy, stop that
                                    if (this.lastTPCIPConnected != null && this.lastTPCIPConnected.USBSerialNumber == this.reconnectionToVerify.USBSerialNumber)
                                        {
                                        ForgetLastTCPIPDevice();
                                        }

                                    // And he's no longer a potential *later* reconnection
                                    if (potentialLastDevice == device)
                                        potentialLastDevice = null;                                    
                                    }
                                
                                // Verification of reconnection is complete
                                this.reconnectionToVerify = null;    
                                }
                            }
                        }
                    else
                        {
                        // ADB doesn't already have a TCPIP connection for him. We'll try to make one if we can.
                        //
                        if (string.IsNullOrEmpty(device.WlanIpAddress))
                            {
                            NotifyNoIpAddress(device);
                            }
                        else if (!device.WlanIsRunning)
                            {
                            NotifyWifiNotRunning(device);    
                            }
                        else
                            {
                            // He's not already connected on an IP network. But can we reach him?
                            Ping        ping    = new Ping();
                            IPAddress   address = IPAddress.Parse(device.WlanIpAddress);
                            PingReply   reply   = ping.Send(address, msPingTimeout);
                            if (reply?.Status == IPStatus.Success)
                                {
                                // Connect to him
                                if (SendTcpipCommandAndConnect(device))
                                    {
                                    connectedAny = true;
                                    }
                                }
                            else
                                NotifyNotPingable(device);
                            }
                        }
                    }

                // If we didn't do any connection here, remember something that ADB is ALREADY
                // connected to as a potential reconnection target for later
                if (!connectedAny && potentialLastDevice != null)
                    {
                    RememberLastTCPIPDevice(potentialLastDevice);
                    }

               this.tracer.Trace($"^-----EnsureAdbDevicesAreOnTCPIP({reason})-----^"); 
               }

            return serverKnowsAboutAnyDevices;
            }

        bool SendTcpipCommandAndConnect(AndroidDevice device)
            {
            bool result = false;
            string ipAddress = device.WlanIpAddress;

            bool tcpipIssuedOk = true;
            try {
                // Restart the device listening on a port of interest. We don't know if he got there,
                // as we get no response from the command issued.
                this.tracer.Trace($"   restarting {device.USBSerialNumber} adbd in TCPIP at {ipAddress}");
                AdbHelper.Instance.TcpIp(AndroidDebugBridge.AdbServerSocketAddress, device.USBSerialNumber, adbdPort);
                }
            catch (Exception)
                {
                this.tracer.Trace($"   restart of {device.USBSerialNumber} adbd failed");
                tcpipIssuedOk = false;
                }
                    
            if (tcpipIssuedOk)
                {
                // Give it a chance to restart. The actual time used here is a total guess, but
                // it does seem to work. Mostly (?).
                Thread.Sleep(1000);

                // Connect to the TCPIP version of that device
                this.tracer.Trace($"   connecting to restarted {ipAddress} device");
                if (AdbHelper.Instance.Connect(AndroidDebugBridge.AdbServerSocketAddress, ipAddress, adbdPort))
                    {
                    NotifyConnected(Resources.NotifyConnected, device, ipAddress);

                    // Remember to whom we last connected for later ADB Server restarts
                    RememberLastTCPIPDevice(device);
                    result = true;
                    }
                }
            
            if (!result)
                {
                this.tracer.Trace($"   failed to connect to {ipAddress}:{adbdPort}");
                NotifyConnected(Resources.NotifyConnectedFail, device, ipAddress);
                }

            return result;
            }

        void ReconnectToLastTCPIPDevice()
        // Attempt to reconnect to the TCPIP device we last saw
            {
            lock (this.deviceConnectionLock)
                {
                if (this.lastTPCIPConnected != null)
                    {
                    string ipAddress = this.lastTPCIPConnected.WlanIpAddress;
                    int portNumber   = adbdPort;

                    this.tracer.Trace($"   reconnecting to {this.lastTPCIPConnected.UserIdentifier}:{this.lastTPCIPConnected.USBSerialNumber} on {this.lastTPCIPConnected.WlanIpAddress}");
                    if (AdbHelper.Instance.Connect(AndroidDebugBridge.AdbServerSocketAddress, ipAddress, portNumber))
                        {
                        // Ok, we connected. But is it the same guy? We'll have to check later. We
                        // snarf away a copy of the device so we'll be able to compare it's current 
                        // state to what we find later; if we didn't copy, then this state could be
                        // tarnished by the 'update to latest connected devices' step.
                        NotifyReconnected(Resources.NotifyReconnected, this.lastTPCIPConnected.UserIdentifier, ipAddress);
                        this.reconnectionToVerify = this.lastTPCIPConnected.DisconnectedCopy();
                        }
                    else
                        {
                        NotifyReconnected(Resources.NotifyReconnectedFail, this.lastTPCIPConnected.UserIdentifier, ipAddress);
                        }
                    }
                }
            }

        //-----------------------------------------------------------------------------------------
        // Communicating with user mode
        //-----------------------------------------------------------------------------------------

        void UpdateStatusRememberedConnection()
            {
            UpdateStatus(string.Format(Resources.LastConnectedToMessage, this.lastTPCIPConnected.UserIdentifier));
            }
        void UpdateStatusNoRememberedConnections()
            {
            UpdateStatus(string.Format(Resources.NoLastConnectedToMessage));
            }
        void NotifyNoIpAddress(AndroidDevice device)
            {
            NotifyMessage(string.Format(Resources.NotifyNoIPAddress, device.UserIdentifier, device.WlanIpAddress));
            }
        void NotifyWifiNotRunning(AndroidDevice device)
            {
            NotifyMessage(string.Format(Resources.NotifyWifiOff, device.UserIdentifier, device.WlanIpAddress));
            }
        void NotifyNotPingable(AndroidDevice device)
            {
            NotifyMessage(string.Format(Resources.NotifyNotPingable, device.UserIdentifier, device.WlanIpAddress));
            }
        void NotifyConnected(string format, AndroidDevice device, string ipAddress)
            {
            NotifyMessage(string.Format(format, device.UserIdentifier, ipAddress));
            }
        void NotifyReconnected(string format, string identifier, string ipAddress)
            {
            NotifyMessage(string.Format(format, identifier, ipAddress));
            }
        void NotifyArmed()
            {
            NotifyMessage(Resources.NotifyArmed);
            }
        void NotifyDisarmed()
            {
            NotifyMessage(Resources.NotifyDisarmed);
            }

        void NotifyMessage(string message)
            {
            this.tracer.Trace($"   notify: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotMessage, message, 100);
            }
        void UpdateStatus(string message)
            {
            message = $"({(this.armed ? Resources.WordArmed : Resources.WordDisarmed)})\n{message}";
            this.tracer.Trace($"   status: {message}");
            this.bugbotMessageQueue.Write(TaggedBlob.TagBugBotStatus, message, 100);
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
                    case TaggedBlob.TagSwerveToolsTrayStarted:
                        UpdateTrayStatus();
                        break;
                    case TaggedBlob.TagDisarmService:
                        this.armed = false;
                        NotifyDisarmed();
                        UpdateTrayStatus();
                        break;
                    case TaggedBlob.TagArmService:
                        this.armed = true;
                        NotifyArmed();
                        UpdateTrayStatus();
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


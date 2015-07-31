using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Library
    {
    public interface ITracer
        { 
        void Trace(string format, params Object[] args);
        }

    public unsafe class DeviceEventArgs : EventArgs
        {
        public WIN32.DEV_BROADCAST_HDR* pHeader;
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

    public class USBDevice
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public Guid     GuidDeviceInterface;
        public String   Name;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public unsafe USBDevice(WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            this.GuidDeviceInterface = pintf->dbcc_classguid;
            this.Name                = pintf->dbcc_name;
            }

        public unsafe USBDevice(Guid interfaceGuid, string name)
            {
            this.GuidDeviceInterface = interfaceGuid;
            this.Name                = name;
            }
        }


    public class USBMonitor : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        IDeviceEvents   eventRaiser = null;
        ITracer         tracer      = null;
        bool            started     = false;
        IntPtr          notificationHandle;
        bool            notificationHandleIsService;

        object                                              traceLock = new object();

        object                                              theLock = new object();
        IDictionary<Guid, IDictionary<String, USBDevice>>   mpGuidDevices = null;
        IDictionary<String, USBDevice>                      mpNameDevice  = null;
        List<Guid>                                          deviceInterfacesOfInterest = null;
        List<IntPtr>                                        deviceNotificationHandles = null;

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
                this.mpGuidDevices = new Dictionary<Guid, IDictionary<String, USBDevice>>();
                this.mpNameDevice  = this.NewMapStringToDevice();
                this.deviceInterfacesOfInterest = new List<Guid>();
                this.deviceNotificationHandles = new List<IntPtr>();
                }
            this.started = false;
            }

        IDictionary<String, USBDevice> NewMapStringToDevice()
            {
            return new Dictionary<String, USBDevice>(StringComparer.InvariantCultureIgnoreCase);
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
                GetDeviceNotificationsFor(guid);
                FindExistingDevices(guid);
                }
            }

        void GetDeviceNotificationsFor(Guid guidDevInterface)
            {
            lock (theLock)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_MANAGED filter = new WIN32.DEV_BROADCAST_DEVICEINTERFACE_MANAGED();
                filter.Initialize(guidDevInterface);

                IntPtr hDeviceNotify = WIN32.RegisterDeviceNotification(this.notificationHandle, filter, this.notificationHandleIsService ? WIN32.DEVICE_NOTIFY_SERVICE_HANDLE : WIN32.DEVICE_NOTIFY_WINDOW_HANDLE);
                WIN32.ThrowIfFail(hDeviceNotify);

                this.deviceNotificationHandles.Add(hDeviceNotify);
                }
            }

        void ReleaseDeviceNotificationHandles()
            {
            lock (theLock)
                {
                foreach (IntPtr hDeviceNotify in this.deviceNotificationHandles)
                    {
                    WIN32.UnregisterDeviceNotification(hDeviceNotify);
                    }
                this.deviceNotificationHandles = new List<IntPtr>();
                }
            }

        public void Start()
            {
            try
                {
                this.eventRaiser.DeviceArrived        += OnDeviceArrived;
                this.eventRaiser.DeviceRemoveComplete += OnDeviceRemoveComplete;

                foreach (Guid guid in this.deviceInterfacesOfInterest)
                    {
                    GetDeviceNotificationsFor(guid);
                    FindExistingDevices(guid);
                    }
                
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
            }


        //-----------------------------------------------------------------------------------------
        // Device Management
        //-----------------------------------------------------------------------------------------

        public unsafe void AddDeviceIfNecessary(WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            AddDeviceIfNecessary(new USBDevice(pintf));
            }

        public unsafe bool RemoveDeviceIfNecessary(WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            return RemoveDeviceIfNecessary(new USBDevice(pintf));
            }

        public void AddDeviceIfNecessary(USBDevice device)
            {
            lock (theLock)
                {
                if (this.deviceInterfacesOfInterest.Contains(device.GuidDeviceInterface))
                    {
                    if (!this.mpNameDevice.ContainsKey(device.Name))
                        {
                        this.mpNameDevice[device.Name] = device;
                        if (!this.mpGuidDevices.ContainsKey(device.GuidDeviceInterface))
                            {
                            this.mpGuidDevices[device.GuidDeviceInterface] = this.NewMapStringToDevice();
                            }
                        this.mpGuidDevices[device.GuidDeviceInterface][device.Name] = device;
                        Trace("added", device);
                        }
                    }
                }
            }

        public bool RemoveDeviceIfNecessary(USBDevice device)
            {
            lock (theLock)
                {
                if (this.mpNameDevice.Remove(device.Name))
                    {
                    this.mpGuidDevices[device.GuidDeviceInterface].Remove(device.Name);
                    Trace("removed", device);
                    return true;
                    }
                }
            return false;
            }

        //-----------------------------------------------------------------------------------------
        // Scanning
        //-----------------------------------------------------------------------------------------

        void FindExistingDevices(Guid guidInterfaceClass)
            {
            IntPtr hDeviceInfoSet = WIN32.INVALID_HANDLE_VALUE;
            try 
                {
                hDeviceInfoSet = WIN32.SetupDiGetClassDevs(ref guidInterfaceClass, IntPtr.Zero, IntPtr.Zero, WIN32.DIGCF_PRESENT | WIN32.DIGCF_DEVICEINTERFACE);
                if (WIN32.INVALID_HANDLE_VALUE==hDeviceInfoSet)
                    WIN32.ThrowWin32Error();

                WIN32.SP_DEVICE_INTERFACE_DATA did = new WIN32.SP_DEVICE_INTERFACE_DATA();
                did.Initialize();

                for (int iMember=0 ;; iMember++)
                    {
                    // Get did of the next interface
                    bool fSuccess = WIN32.SetupDiEnumDeviceInterfaces
                        (hDeviceInfoSet,
                        IntPtr.Zero,
                        ref guidInterfaceClass,
                        iMember,
                        out did);

                    if (!fSuccess)
                        {
                        break;  // Done! no more 
                        }
                    else
                        {
                        // A device is present. Get details
                        WIN32.SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED detail = new WIN32.SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED();
                        detail.Initialize();

                        int cbRequired;
                        WIN32.ThrowIfFail(WIN32.SetupDiGetDeviceInterfaceDetail
                            (hDeviceInfoSet,
                            ref did,
                            ref detail,
                            Marshal.SizeOf(detail),
                            out cbRequired,
                            IntPtr.Zero));

                        USBDevice device = new USBDevice(did.InterfaceClassGuid, detail.DevicePath);
                        this.AddDeviceIfNecessary(device);
                        }

                    }
                }
            finally
                { 
                if (hDeviceInfoSet != IntPtr.Zero && hDeviceInfoSet != WIN32.INVALID_HANDLE_VALUE)
                    {
                    WIN32.SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
                    }
                }
            }


        //-----------------------------------------------------------------------------------------
        // Events
        //-----------------------------------------------------------------------------------------

        unsafe void OnDeviceArrived(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == WIN32.DBT_DEVTYP_DEVICEINTERFACE)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (WIN32.DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                // Trace(pintf);
                this.AddDeviceIfNecessary(new USBDevice(pintf));
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == WIN32.DBT_DEVTYP_DEVICEINTERFACE)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (WIN32.DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                // Trace(pintf);
                this.RemoveDeviceIfNecessary(new USBDevice(pintf));
                }
            }

        unsafe void Trace(WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            lock (traceLock)
                {
                this.tracer.Trace("    pintf->size={0}", pintf->dbcc_size);
                this.tracer.Trace("    pintf->name={0}", pintf->dbcc_name);
                this.tracer.Trace("    pintf->guid={0}", pintf->dbcc_classguid);
                }
            }

        void Trace(string message, USBDevice device)
            {
            lock (traceLock)
                {
                this.tracer.Trace("{0}: ", message);
                this.tracer.Trace("    name={0}", device.Name);
                this.tracer.Trace("    guid={0}", device.GuidDeviceInterface);
                }
            }
        }
    }


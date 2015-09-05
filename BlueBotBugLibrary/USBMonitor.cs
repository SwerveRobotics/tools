using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.SwerveRobotics.Tools.Library.WIN32;

namespace Org.SwerveRobotics.Tools.Library
    {
    public interface ITracer
        { 
        void Trace(string format, params object[] args);
        void Trace(string message, USBDeviceInterface device);
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

    /// <summary>
    /// A USBDeviceInformationElement represents a USB device information element
    /// </summary>
    /// Helpful links:
    ///     device id:               https://msdn.microsoft.com/en-us/library/windows/hardware/ff537109(v=vs.85).aspx
    ///     device information sets: https://msdn.microsoft.com/EN-US/library/windows/hardware/ff541247(v=vs.85).aspx
    public class USBDeviceInformationElement
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public string                       DeviceInstanceId = null;
        public List<USBDeviceInterface>     Interfaces = new List<USBDeviceInterface>();
        }


    public class USBDeviceInterface
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public readonly Guid     GuidDeviceInterface;
        public readonly string   DeviceInterfacePath;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public USBDeviceInterface()
            {
            this.GuidDeviceInterface = Guid.Empty;
            this.DeviceInterfacePath = null;
            }

        public unsafe USBDeviceInterface(bool deviceAdded, DEV_BROADCAST_DEVICEINTERFACE_W* pintf) : this(deviceAdded, pintf->dbcc_classguid, pintf->dbcc_name)
            {
            }

        public USBDeviceInterface(bool deviceAdded, Guid interfaceGuid, string deviceInterfacePath)
            {
            this.GuidDeviceInterface = interfaceGuid;
            this.DeviceInterfacePath = deviceInterfacePath;
            }
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
        readonly object     traceLock = new object();

        readonly object                                             theLock = new object();
        IDictionary<Guid, IDictionary<string, USBDeviceInterface>>  mpGuidDevices = null;
        IDictionary<string, USBDeviceInterface>                     mpNameDevice  = null;
        List<Guid>                                                  deviceInterfacesOfInterest = null;
        List<IntPtr>                                                deviceNotificationHandles = null;

        public EventHandler<USBDeviceInterface> OnDeviceOfInterestArrived;
        public EventHandler<USBDeviceInterface> OnDeviceOfInterestRemoved;

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
                this.mpGuidDevices = new Dictionary<Guid, IDictionary<string, USBDeviceInterface>>();
                this.mpNameDevice  = this.NewMapStringToDevice();
                this.deviceInterfacesOfInterest = new List<Guid>();
                this.deviceNotificationHandles = new List<IntPtr>();
                }
            this.started = false;
            }

        IDictionary<string, USBDeviceInterface> NewMapStringToDevice()
            {
            return new Dictionary<string, USBDeviceInterface>(StringComparer.InvariantCultureIgnoreCase);
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

        public unsafe void AddDeviceIfNecessary(DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            AddDeviceIfNecessary(new USBDeviceInterface(true, pintf));
            }

        public unsafe bool RemoveDeviceIfNecessary(DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            return RemoveDeviceIfNecessary(new USBDeviceInterface(false, pintf));
            }

        public void AddDeviceIfNecessary(USBDeviceInterface device)
            {
            EnumerateDevicesContainingInterface(deviceInterfacesOfInterest[0]);

            lock (theLock)
                {
                if (this.deviceInterfacesOfInterest.Contains(device.GuidDeviceInterface))
                    {
                    if (!this.mpNameDevice.ContainsKey(device.DeviceInterfacePath))
                        {
                        this.mpNameDevice[device.DeviceInterfacePath] = device;
                        if (!this.mpGuidDevices.ContainsKey(device.GuidDeviceInterface))
                            {
                            this.mpGuidDevices[device.GuidDeviceInterface] = this.NewMapStringToDevice();
                            }
                        this.mpGuidDevices[device.GuidDeviceInterface][device.DeviceInterfacePath] = device;
                        Trace("added", device);
                        this.OnDeviceOfInterestArrived.Invoke(null, device);
                        }
                    }
                }
            }

        public bool RemoveDeviceIfNecessary(USBDeviceInterface device)
            {
            lock (theLock)
                {
                if (this.mpNameDevice.Remove(device.DeviceInterfacePath))
                    {
                    this.mpGuidDevices[device.GuidDeviceInterface].Remove(device.DeviceInterfacePath);
                    Trace("removed", device);
                    this.OnDeviceOfInterestRemoved.Invoke(null, device);
                    return true;
                    }
                }
            return false;
            }

        //-----------------------------------------------------------------------------------------
        // Scanning
        //-----------------------------------------------------------------------------------------

        //  “adb shell netcfg” | qgrep -y wlan
        // adb shell ifconfig wlan0

        void EnumerateDevicesContainingInterface(Guid guidInterfaceClass)
        // Device information sets: https://msdn.microsoft.com/EN-US/library/windows/hardware/ff541247(v=vs.85).aspx
            {
            List<USBDeviceInformationElement> result = new List<USBDeviceInformationElement>();

            IntPtr hDeviceInfoSet = INVALID_HANDLE_VALUE;
            try 
                {
                // Query for every device information element in the system
                hDeviceInfoSet = SetupDiGetClassDevsW(ref guidInterfaceClass, "USB", IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
                if (INVALID_HANDLE_VALUE != hDeviceInfoSet)
                    {
                    int cbRequired;

                    // Enumerate all those devices
                    for (int iDevInfo = 0; ; iDevInfo++)
                        {
                        SP_DEVINFO_DATA devInfo = SP_DEVINFO_DATA.Construct();
                        if (SetupDiEnumDeviceInfo(hDeviceInfoSet, iDevInfo, ref devInfo))
                            {
                            // Retrieve the device instance ID that is associated with the current device information element
                            cbRequired = 2;     // arbitrary
                            IntPtr pbDeviceInstanceId = Marshal.AllocCoTaskMem(cbRequired);
                            if (!SetupDiGetDeviceInstanceIdW(hDeviceInfoSet, ref devInfo, IntPtr.Zero, cbRequired, out cbRequired))
                                {
                                Marshal.FreeCoTaskMem(pbDeviceInstanceId);
                                pbDeviceInstanceId = Marshal.AllocCoTaskMem(cbRequired);
                                }
                            try {
                                ThrowIfFail(SetupDiGetDeviceInstanceIdW(hDeviceInfoSet, ref devInfo, pbDeviceInstanceId, cbRequired, out cbRequired));
                                string deviceInstanceId = Marshal.PtrToStringUni(pbDeviceInstanceId);

                                USBDeviceInformationElement device = new USBDeviceInformationElement() { DeviceInstanceId = deviceInstanceId };
                                result.Add(device);

                                // Enumerate the interfaces of that device information element
                                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = SP_DEVICE_INTERFACE_DATA.Construct();
                                for (int iInterface=0 ;; iInterface++)
                                    {
                                    if (SetupDiEnumDeviceInterfaces(hDeviceInfoSet, ref devInfo, ref guidInterfaceClass, iInterface, ref deviceInterfaceData))
                                        {
                                        // Retrieve the device path of that interfae
                                        SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED devicePath = SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED.Construct();
                                        ThrowIfFail(SetupDiGetDeviceInterfaceDetail(hDeviceInfoSet, ref deviceInterfaceData, ref devicePath, Marshal.SizeOf(devicePath), out cbRequired, IntPtr.Zero));

                                        USBDeviceInterface deviceInterface = new USBDeviceInterface(true, deviceInterfaceData.InterfaceClassGuid, devicePath.DevicePath);
                                        device.Interfaces.Add(deviceInterface);
                                        }
                                    else
                                        break; // interface enumeration complete
                                    }
                                }
                            finally
                                {
                                Marshal.FreeCoTaskMem(pbDeviceInstanceId);
                                }
                            }
                        else
                            break;  // device enumeration complete
                        }
                    }
                else
                    ThrowWin32Error();
                }
            finally
                {
                // Clean up the device enumeration
                if (hDeviceInfoSet != IntPtr.Zero && hDeviceInfoSet != INVALID_HANDLE_VALUE)
                    {
                    SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
                    }
                }
            }

        void FindExistingDevices(Guid guidInterfaceClass)
            {
            IntPtr hDeviceInfoSet = INVALID_HANDLE_VALUE;
            try 
                {
                hDeviceInfoSet = SetupDiGetClassDevs(ref guidInterfaceClass, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                if (INVALID_HANDLE_VALUE==hDeviceInfoSet)
                    ThrowWin32Error();

                SP_DEVICE_INTERFACE_DATA did = SP_DEVICE_INTERFACE_DATA.Construct();

                for (int iInterface=0 ;; iInterface++)
                    {
                    // Get did of the next interface
                    bool fSuccess = SetupDiEnumDeviceInterfaces
                        (hDeviceInfoSet,
                        IntPtr.Zero,        // change
                        ref guidInterfaceClass,
                        iInterface,
                        ref did);

                    if (!fSuccess)
                        {
                        break;  // Done! no more 
                        }
                    else
                        {
                        // A device is present. Get details
                        SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED detail = SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED.Construct();

                        int cbRequired;
                        ThrowIfFail(SetupDiGetDeviceInterfaceDetail
                            (hDeviceInfoSet,
                            ref did,
                            ref detail,
                            Marshal.SizeOf(detail),
                            out cbRequired,
                            IntPtr.Zero));

                        USBDeviceInterface device = new USBDeviceInterface(true, did.InterfaceClassGuid, detail.DevicePath);
                        this.AddDeviceIfNecessary(device);
                        }

                    }
                }
            finally
                { 
                if (hDeviceInfoSet != IntPtr.Zero && hDeviceInfoSet != INVALID_HANDLE_VALUE)
                    {
                    SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
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
                // Trace(pintf);
                this.AddDeviceIfNecessary(new USBDeviceInterface(true, pintf));
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                // Trace(pintf);
                this.RemoveDeviceIfNecessary(new USBDeviceInterface(false, pintf));
                }
            }

        unsafe void Trace(DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            lock (traceLock)
                {
                this.tracer.Trace("    pintf->size={0}", pintf->dbcc_size);
                this.tracer.Trace("    pintf->DevicePath={0}", pintf->dbcc_name);
                this.tracer.Trace("    pintf->guid={0}", pintf->dbcc_classguid);
                }
            }

        void Trace(string message, USBDeviceInterface device)
            {
            lock (traceLock)
                {
                this.tracer.Trace("{0}: ", message);
                this.tracer.Trace("    DevicePath={0}", device.DeviceInterfacePath);
                this.tracer.Trace("    guid={0}", device.GuidDeviceInterface);
                }
            }
        }
    }


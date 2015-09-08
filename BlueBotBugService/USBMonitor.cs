using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


    public class USBDeviceInterface
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public Guid    GuidDeviceInterface;
        public string  DeviceInterfacePath;
        public string  SerialNumber; 

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public USBDeviceInterface()
            {
            this.GuidDeviceInterface = Guid.Empty;
            this.DeviceInterfacePath = null;
            this.SerialNumber        = null;
            }

        public USBDeviceInterface(Guid interfaceGuid, string deviceInterfacePath, string serialNumber)
            {
            this.GuidDeviceInterface = interfaceGuid;
            this.DeviceInterfacePath = deviceInterfacePath.ToLowerInvariant();
            this.SerialNumber        = serialNumber;
            }

        public unsafe USBDeviceInterface(DEV_BROADCAST_DEVICEINTERFACE_W* pintf, string serialNumber)
            {
            this.GuidDeviceInterface = pintf->dbcc_classguid;
            this.DeviceInterfacePath = pintf->dbcc_name.ToLowerInvariant();
            this.SerialNumber        = serialNumber;
            }

        public override bool Equals(object obj)
            {
            if (obj is USBDeviceInterface)
                return this.DeviceInterfacePath == (obj as USBDeviceInterface).DeviceInterfacePath;
            else
                return false;
            }
        public override int GetHashCode()
            {
            return this.DeviceInterfacePath.GetHashCode() ^ 1398713;
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

        readonly object     theLock = new object();
        HashSet<USBDeviceInterface>     currentDevices = null;
        List<Guid>                      deviceInterfacesOfInterest = null;
        List<IntPtr>                    deviceNotificationHandles = null;

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
                this.currentDevices = new HashSet<USBDeviceInterface>();
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

        public bool AddDeviceIfNecessary(USBDeviceInterface device)
            {
            bool result = false;
            lock (theLock)
                {
                if (this.deviceInterfacesOfInterest.Contains(device.GuidDeviceInterface))
                    {
                    if (this.currentDevices.Add(device))
                        {
                        result = true;
                        Trace("added", device);
                        }
                    }
                }
            return result;
            }

        public bool RemoveDeviceIfNecessary(USBDeviceInterface device)
            {
            bool result = false;
            lock (theLock)
                {
                foreach (USBDeviceInterface him in this.currentDevices)
                    {
                    if (him.DeviceInterfacePath == device.DeviceInterfacePath)
                        {
                        device.SerialNumber = him.SerialNumber;
                        break;
                        }
                    }
                if (this.currentDevices.Remove(device))
                    {
                    result = true;
                    Trace("removed", device);
                    }
                }
            return result;
            }

        //-----------------------------------------------------------------------------------------
        // Scanning
        //-----------------------------------------------------------------------------------------

        void FindExistingDevices(Guid guidInterface)
            {
            HashSet<USBDeviceInterface> devices = GetSerialNumbersofDevices(guidInterface);
            foreach (USBDeviceInterface device in devices)
                {
                AddDeviceIfNecessary(device);
                }
            }

        public string SerialNumberOfDeviceInterface(string path)
        // Given a path to a USB device interface, return the serial number of that device
            {
            string result = null;
            IntPtr h = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                                  FILE_SHARE_READ | FILE_SHARE_WRITE,
                                  IntPtr.Zero, OPEN_EXISTING,
                                  FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (h != INVALID_HANDLE_VALUE)
                {
                try
                    {
                    // Convert the file handle to a WinUSB handle
                    IntPtr usbHandle;
                    if (WinUsb_Initialize(h, out usbHandle))
                        {
                        try {
                            // Get the device descriptor; that will give us a serial number 'index'
                            int cbCopied;
                            USB_DEVICE_DESCRIPTOR usbDeviceDescriptor = new USB_DEVICE_DESCRIPTOR();
                            if (WinUsb_GetDescriptor(usbHandle, USB_DEVICE_DESCRIPTOR_TYPE, 0, 0, ref usbDeviceDescriptor, Marshal.SizeOf(usbDeviceDescriptor), out cbCopied))
                                {
                                // Exchange that 'index' for a string. Unfortunately, WinUsb_GetDescriptor wont ever tell us how big of 
                                // buffer it actually needs, so we just grow until we get big enough
                                int cbBuffer = 64;
                                IntPtr pbBuffer = Marshal.AllocCoTaskMem(cbBuffer);
                                while (!WinUsb_GetDescriptor(usbHandle, USB_STRING_DESCRIPTOR_TYPE, usbDeviceDescriptor.iSerialNumber, 0x409, pbBuffer, cbBuffer, out cbCopied))
                                    {
                                    if (GetLastError()==ERROR_INSUFFICIENT_BUFFER)
                                        {
                                        Marshal.FreeCoTaskMem(pbBuffer);
                                        cbBuffer *= 2;
                                        }
                                    else
                                        ThrowWin32Error("WinUsb_GetDescriptor failed");
                                    }

                                result = Marshal.PtrToStringUni(pbBuffer+USB_STRING_DESCRIPTOR.CbOverhead, (cbCopied-USB_STRING_DESCRIPTOR.CbOverhead)/2);
                                Marshal.FreeCoTaskMem(pbBuffer);
                                }
                            }
                        finally
                            {
                            WinUsb_Free(usbHandle);
                            }
                        }
                    else
                        {
                        ThrowWin32Error("failed to open WinUsb_Initialize");
                        }
                    }
                finally
                    {
                    CloseHandle(h);
                    }
                }
            else
                {
                // This can be caused by ADB getting into a weird state
                ThrowWin32Error("failed to open device");
                }

            return result;
            }

        HashSet<USBDeviceInterface> GetSerialNumbersofDevices(Guid guidInterfaceClass)
        // Return the set of serial numbers of currently connected USB devices which have the indicated interface class
            {
            HashSet<USBDeviceInterface> result = new HashSet<USBDeviceInterface>();

            // Get a set consisting of the USB devices that have the device interface of interest
            IntPtr hDeviceInfoSet = SetupDiGetClassDevsW(ref guidInterfaceClass, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (hDeviceInfoSet != INVALID_HANDLE_VALUE)
                {
                try {
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = SP_DEVICE_INTERFACE_DATA.Construct();
                    for (int iDevice=0 ;; iDevice++)
                        {
                        // Iterate over that device interface set
                        if (SetupDiEnumDeviceInterfaces(hDeviceInfoSet, IntPtr.Zero, ref guidInterfaceClass, iDevice, ref deviceInterfaceData))
                            {
                            // Get the path to the next device
                            SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED detail = SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED.Construct();
                            int cbRequired;
                            if (SetupDiGetDeviceInterfaceDetail(hDeviceInfoSet, ref deviceInterfaceData, ref detail, Marshal.SizeOf(detail), out cbRequired, IntPtr.Zero))
                                {
                                // From that path, retrieve the USB serial number
                                string serialNumber = SerialNumberOfDeviceInterface(detail.DevicePath);
                                result.Add(new USBDeviceInterface(guidInterfaceClass, detail.DevicePath, serialNumber));
                                }
                            else
                                ThrowWin32Error();
                            }
                        else if (GetLastError() == ERROR_NO_MORE_ITEMS)
                            break;
                        else
                            ThrowWin32Error();
                        }
                    }
                finally
                    { 
                    SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
                    }
                }
            return result;
            }

        //-----------------------------------------------------------------------------------------
        // Win32 Events
        //-----------------------------------------------------------------------------------------

        unsafe void OnDeviceArrived(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                this.AddDeviceIfNecessary(new USBDeviceInterface(pintf, this.SerialNumberOfDeviceInterface(pintf->dbcc_name)));
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                this.RemoveDeviceIfNecessary(new USBDeviceInterface(pintf, null));
                }
            }

        unsafe void Trace(DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            lock (traceLock)
                {
                this.tracer.Trace("    pintf->size={0}",        pintf->dbcc_size);
                this.tracer.Trace("    pintf->DevicePath={0}",  pintf->dbcc_name);
                this.tracer.Trace("    pintf->guid={0}",        pintf->dbcc_classguid);
                }
            }

        void Trace(string message, USBDeviceInterface device)
            {
            lock (traceLock)
                {
                this.tracer.Trace("{0}: ", message);
                this.tracer.Trace("    devicePath={0}",     device.DeviceInterfacePath);
                this.tracer.Trace("    guid={0}",           device.GuidDeviceInterface);
                this.tracer.Trace("    serialNumber={0}",   device.SerialNumber);
                }
            }
        }
    }


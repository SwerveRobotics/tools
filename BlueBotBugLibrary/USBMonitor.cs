using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Library
    {
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


    public class USBMonitor
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        BlueBotBug  bug    = null;
        ITracer     tracer = null;

        object      theLock = new object();
        object      traceLock = new object();
        IDictionary<Guid, IDictionary<String, USBDevice>>   mpGuidDevices = null;
        IDictionary<String, USBDevice>                      mpNameDevice  = null;

        // http://binarydb.com/driver/Android-ADB-Interface-265790.html

        public static Guid AndroidUsbDeviceClass     = new Guid("{3f966bd9-fa04-4ec5-991c-d326973b5128}");
        public static Guid AndroidADBDeviceInterface = new Guid("{F72FE0D4-CBCB-407D-8814-9ED673D0DD6B}");

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public USBMonitor(BlueBotBug bug, ITracer tracer)
            {
            this.bug = bug;
            this.tracer = tracer;
            this.Initialize();
            }

        void Initialize()
            {
            lock (theLock)
                {
                this.mpGuidDevices = new Dictionary<Guid, IDictionary<String, USBDevice>>();
                this.mpNameDevice  = this.NewMapStringToDevice();
                }
            }

        IDictionary<String, USBDevice> NewMapStringToDevice()
            {
            return new Dictionary<String, USBDevice>(StringComparer.InvariantCultureIgnoreCase);
            }

        public void Start()
            {
            this.bug.DeviceArrived        += OnDeviceArrived;
            this.bug.DeviceRemoveComplete += OnDeviceRemoveComplete;

            // TODO: Generalize this so that we find other devices as well
            FindDevices(AndroidADBDeviceInterface);
            FindDevices(WIN32.GUID_DEVINTERFACE_USB_DEVICE);
            }

        public void Stop()
            {
            this.bug.DeviceArrived        -= OnDeviceArrived;
            this.bug.DeviceRemoveComplete -= OnDeviceRemoveComplete;
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

        void FindDevices(Guid guidInterfaceClass)
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
                        WIN32.SP_DEVICE_INTERFACE_DETAIL_DATA detail = new WIN32.SP_DEVICE_INTERFACE_DETAIL_DATA();
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

        unsafe void OnDeviceArrived(object sender, BlueBotBug.DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == WIN32.DBT_DEVTYP_DEVICEINTERFACE)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (WIN32.DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                // Trace(pintf);
                this.AddDeviceIfNecessary(new USBDevice(pintf));
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, BlueBotBug.DeviceEventArgs args)
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


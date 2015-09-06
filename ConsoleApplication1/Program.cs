using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApplication1.WIN32;

namespace ConsoleApplication1
    {
    class Program
        {
        public readonly static Guid AndroidADBDeviceInterface = new Guid("{F72FE0D4-CBCB-407D-8814-9ED673D0DD6B}");

        static void Main(string[] args)
            {
            if (false)
                { 
                string path = "\\\\?\\usb#vid_19d2&pid_1351&mi_01#7&6f0a7d6&1&0001#{f72fe0d4-cbcb-407d-8814-9ed673d0dd6b}";
                IntPtr h = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                                      FILE_SHARE_READ | FILE_SHARE_WRITE,
                                      IntPtr.Zero, OPEN_EXISTING,
                                      FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                if (h != INVALID_HANDLE_VALUE)
                    {
                    IntPtr usbHandle;
                    if (WinUsb_Initialize(h, out usbHandle))
                        {
                        try {
                            int cbCopied;
                            USB_DEVICE_DESCRIPTOR usbDeviceDescriptor = new USB_DEVICE_DESCRIPTOR();
                            if (WinUsb_GetDescriptor(usbHandle, USB_DEVICE_DESCRIPTOR_TYPE, 0, 0, ref usbDeviceDescriptor, Marshal.SizeOf(usbDeviceDescriptor), out cbCopied))
                                {
                                int cbBuffer = 64;
                                IntPtr pbBuffer = Marshal.AllocCoTaskMem(cbBuffer);
                                while (!WinUsb_GetDescriptor(usbHandle, USB_STRING_DESCRIPTOR_TYPE, usbDeviceDescriptor.iSerialNumber, 0x409, pbBuffer, cbBuffer, out cbCopied))
                                    {
                                    if (Marshal.GetLastWin32Error()==ERROR_INSUFFICIENT_BUFFER)
                                        {
                                        Marshal.FreeCoTaskMem(pbBuffer);
                                        cbBuffer *= 2;
                                        }
                                    else
                                        ThrowWin32Error();
                                    }

                                string serialNumber = Marshal.PtrToStringUni(pbBuffer);
                                Marshal.FreeCoTaskMem(pbBuffer);
                                }
                            }
                        finally
                            {
                            WinUsb_Free(usbHandle);
                            }
                        }
                    else
                        ThrowWin32Error();

                    CloseHandle(h);
                    }
                }
            FindExistingDevices(AndroidADBDeviceInterface);
            }

        static string SerialNumberOfDeviceInterface(string path)
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
                    IntPtr usbHandle;
                    if (WinUsb_Initialize(h, out usbHandle))
                        {
                        try {
                            int cbCopied;
                            USB_DEVICE_DESCRIPTOR usbDeviceDescriptor = new USB_DEVICE_DESCRIPTOR();
                            if (WinUsb_GetDescriptor(usbHandle, USB_DEVICE_DESCRIPTOR_TYPE, 0, 0, ref usbDeviceDescriptor, Marshal.SizeOf(usbDeviceDescriptor), out cbCopied))
                                {
                                int cbBuffer = 64;
                                IntPtr pbBuffer = Marshal.AllocCoTaskMem(cbBuffer);
                                while (!WinUsb_GetDescriptor(usbHandle, USB_STRING_DESCRIPTOR_TYPE, usbDeviceDescriptor.iSerialNumber, 0x409, pbBuffer, cbBuffer, out cbCopied))
                                    {
                                    if (Marshal.GetLastWin32Error()==ERROR_INSUFFICIENT_BUFFER)
                                        {
                                        Marshal.FreeCoTaskMem(pbBuffer);
                                        cbBuffer *= 2;
                                        }
                                    else
                                        ThrowWin32Error();
                                    }

                                result = Marshal.PtrToStringUni(pbBuffer,cbCopied/2);
                                Marshal.FreeCoTaskMem(pbBuffer);
                                }
                            }
                        finally
                            {
                            WinUsb_Free(usbHandle);
                            }
                        }
                    else
                        ThrowWin32Error();
                    }
                finally
                    {
                    CloseHandle(h);
                    }
                }
            else
                ThrowWin32Error();

            return result;
            }

        static void FindExistingDevices(Guid guidInterfaceClass)
            {
            IntPtr hDeviceInfoSet = WIN32.INVALID_HANDLE_VALUE;
            try 
                {
                hDeviceInfoSet = WIN32.SetupDiGetClassDevsW(ref guidInterfaceClass, IntPtr.Zero, IntPtr.Zero, WIN32.DIGCF_PRESENT | WIN32.DIGCF_DEVICEINTERFACE);
                if (WIN32.INVALID_HANDLE_VALUE==hDeviceInfoSet)
                    WIN32.ThrowWin32Error();

                WIN32.SP_DEVICE_INTERFACE_DATA did = SP_DEVICE_INTERFACE_DATA.Construct();

                for (int iMember=0 ;; iMember++)
                    {
                    // Get did of the next interface
                    bool fSuccess = WIN32.SetupDiEnumDeviceInterfaces
                        (hDeviceInfoSet,
                        IntPtr.Zero,
                        ref guidInterfaceClass,
                        iMember,
                        ref did);

                    if (!fSuccess)
                        {
                        break;  // Done! no more 
                        }
                    else
                        {
                        // A device is present. Get details
                        WIN32.SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED detail = SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED.Construct();

                        int cbRequired;
                        WIN32.ThrowIfFail(WIN32.SetupDiGetDeviceInterfaceDetail
                            (hDeviceInfoSet,
                            ref did,
                            ref detail,
                            Marshal.SizeOf(detail),
                            out cbRequired,
                            IntPtr.Zero));

                        string serialNumber = SerialNumberOfDeviceInterface(detail.DevicePath);
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




        }

    static class WIN32
        {
        [DllImport("winusb.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool WinUsb_Initialize(IntPtr handle, out IntPtr pWinUsbHandle);

        [DllImport("winusb.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool WinUsb_Free(IntPtr usbHandle);

        [DllImport("winusb.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool WinUsb_GetDescriptor
            (
            IntPtr InterfaceHandle,
            byte   DescriptorType,
            byte   Index,
            short  LanguageID,
            IntPtr Buffer,
            int    BufferLength,
            out int LengthTransferred
            );

        [DllImport("winusb.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool WinUsb_GetDescriptor
            (
            IntPtr InterfaceHandle,
            byte   DescriptorType,
            byte   Index,
            short  LanguageID,
            ref USB_DEVICE_DESCRIPTOR Buffer,
            int    BufferLength,
            out int LengthTransferred
            );

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct USB_DEVICE_DESCRIPTOR
            {
            public byte   bLength;
            public byte   bDescriptorType;
            public short  bcdUSB;
            public byte   bDeviceClass;
            public byte   bDeviceSubClass;
            public byte   bDeviceProtocol;
            public byte   bMaxPacketSize0;
            public short  idVendor;
            public short  idProduct;
            public short  bcdDevice;
            public byte   iManufacturer;
            public byte   iProduct;
            public byte   iSerialNumber;
            public byte   bNumConfigurations;
            }

        // selected Win32 errors
        public const int ERROR_INVALID_FUNCTION         = 1;
        public const int ERROR_FILE_NOT_FOUND           = 2;
        public const int ERROR_PATH_NOT_FOUND           = 3;
        public const int ERROR_TOO_MANY_OPEN_FILES      = 4;
        public const int ERROR_ACCESS_DENIED            = 5;
        public const int ERROR_INVALID_HANDLE           = 6;
        public const int ERROR_BAD_COMMAND              = 22;
        public const int ERROR_GEN_FAILURE              = 31;
        public const int ERROR_SHARING_VIOLATION        = 32;
        public const int ERROR_LOCK_VIOLATION           = 33;
        public const int ERROR_INVALID_PARAMETER        = 87;
        public const int ERROR_INSUFFICIENT_BUFFER      = 122;
        public const int ERROR_NO_MORE_ITEMS            = 259;
        public const int ERROR_OPERATION_ABORTED        = 995;
        public const int ERROR_IO_INCOMPLETE            = 996;
        public const int ERROR_IO_PENDING               = 997;
        public const int ERROR_SERVICE_SPECIFIC_ERROR   = 1066;

        //
        // USB 1.1: 9.4 Standard Device Requests, Table 9-5. Descriptor Types
        //
        public const byte USB_DEVICE_DESCRIPTOR_TYPE                          = 0x01;
        public const byte USB_CONFIGURATION_DESCRIPTOR_TYPE                   = 0x02;
        public const byte USB_STRING_DESCRIPTOR_TYPE                          = 0x03;
        public const byte USB_INTERFACE_DESCRIPTOR_TYPE                       = 0x04;
        public const byte USB_ENDPOINT_DESCRIPTOR_TYPE                        = 0x05;

        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public const int DIGCF_DEFAULT           = 0x00000001;  // only valid with DIGCF_DEVICEINTERFACE
        public const int DIGCF_PRESENT           = 0x00000002;
        public const int DIGCF_ALLCLASSES        = 0x00000004;
        public const int DIGCF_PROFILE           = 0x00000008;
        public const int DIGCF_DEVICEINTERFACE   = 0x00000010;

        public const int DEVICE_NOTIFY_WINDOW_HANDLE         = 0x00000000;
        public const int DEVICE_NOTIFY_SERVICE_HANDLE        = 0x00000001;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;

        public const int  MAX_PATH = 260;

        public const int  FILE_ATTRIBUTE_NORMAL       = 0X80;
        public const int  FILE_FLAG_OVERLAPPED        = 0X40000000;
        public const int  FILE_SHARE_READ             = 1;
        public const int  FILE_SHARE_WRITE            = 2;
        public const uint GENERIC_READ                = 0X80000000;
        public const uint GENERIC_WRITE               = 0X40000000;
        public const int  OPEN_EXISTING               = 3;

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="CreateFileW")] public static extern 
        IntPtr CreateFile(string lpFileName, 
            uint    dwDesiredAccess, 
            int     dwShareMode, 
            IntPtr  lpSecurityAttributes, 
            int     dwCreationDisposition, 
            int     dwFlagsAndAttributes, 
            IntPtr  hTemplateFile);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool CloseHandle(IntPtr handle);
        public static void ThrowWin32Error()
            {
            ThrowWin32Error(Marshal.GetLastWin32Error());
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVICE_INTERFACE_DATA
            {
            public int          cbSize;
            public System.Guid  InterfaceClassGuid;
            public int          Flags;
            public IntPtr       Reserved;

            public static SP_DEVICE_INTERFACE_DATA Construct()
                {
                SP_DEVICE_INTERFACE_DATA result;
                result.cbSize             = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                result.InterfaceClassGuid = Guid.Empty;
                result.Flags              = 0;
                result.Reserved           = IntPtr.Zero;
                return result;
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED
            {
            public int          cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string       DevicePath;

            public static SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED Construct()
                {
                SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED result;
                result.cbSize     = Marshal.OffsetOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED), nameof(DevicePath)).ToInt32() + Marshal.SystemDefaultCharSize;   // Odd convention used for this size
                result.DevicePath = null;
                return result;
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVINFO_DATA
            {
            public int          cbSize;
            public System.Guid  ClassGuid;
            public int          DevInst;
            public int          Reserved;

            public static SP_DEVINFO_DATA Construct()
                {
                SP_DEVINFO_DATA result;
                result.cbSize    = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                result.ClassGuid = Guid.Empty;
                result.DevInst   = 0;
                result.Reserved  = 0;
                return result;
                }
            }

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(IntPtr pClassGuid,         IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(ref System.Guid ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiDestroyDeviceInfoList(IntPtr hDeviceInfoSet);

        //[DllImport("setupapi.dll", SetLastError = true)] public static extern 
        //bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, IntPtr DeviceInfoData, IntPtr InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        //[DllImport("setupapi.dll", SetLastError = true)] public static extern 
        //bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern 
        bool SetupDiGetDeviceInterfaceDetail(
            IntPtr                                      hDeviceInfoSet, 
            ref SP_DEVICE_INTERFACE_DATA                DeviceInterfaceData, 
            ref SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED DeviceInterfaceDetailData, 
            int                                         DeviceInterfaceDetailDataSize, 
            out int                                     cbRequired, 
            IntPtr                                      DeviceInfoData
            );

        public static void ThrowIfFail(IntPtr ptr)
            {
            if (IntPtr.Zero == ptr)
                {
                ThrowWin32Error();
                }
            }
        public static void ThrowIfFail(int fSuccess)
            {
            if (fSuccess==0)
                {
                ThrowWin32Error();
                }
            }
        public static void ThrowIfFail(bool fSuccess)
            {
            if (!fSuccess)
                {
                ThrowWin32Error();
                }
            }
        public static void ThrowWin32Error(int err)
            {
            switch (err)
                {
            case 0:
                return;
            default:
                throw new System.ComponentModel.Win32Exception(err);
                }
            }
        }
    }


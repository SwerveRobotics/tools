//
// WIN32.cs
//
// Plumbing for interacting with native code Windows APIs. Some of this
// is rocket science, and it all certainly could be documented better.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using COMTypes = System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32.SafeHandles;

namespace Org.SwerveRobotics.Tools.Library
    {
    //=================================================================================

    public partial class WIN32
        {
        //------------------------------------------------------------------------------
        // Error codes
        //------------------------------------------------------------------------------

        // seleted HRESULTs
        public const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);
        public const int STG_E_FILENOTFOUND    = unchecked((int)0x80030002);
        public const int STG_E_PATHNOTFOUND    = unchecked((int)0x80030003);

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
        public const int ERROR_OPERATION_ABORTED        = 995;
        public const int ERROR_IO_INCOMPLETE            = 996;
        public const int ERROR_IO_PENDING               = 997;
        public const int ERROR_SERVICE_SPECIFIC_ERROR   = 1066;

        //------------------------------------------------------------------------------
        // Structs
        //------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public unsafe struct BLUETOOTH_ADDRESS
            {
            public long ullLong;        // only lower six bytes are valid

            byte this[int ib] { get 
                {
                return (byte)( (this.ullLong >> ((5-ib)*8)) & 0xFF );
                }}

            public override string ToString()
                {
                return String.Format("{0:X02}{1:X02}{2:X02}{3:X02}{4:X02}{5:X02}", this[0], this[1], this[2], this[3], this[4], this[5]);
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_INFO
            {
            public int                  dwSize;
            public BLUETOOTH_ADDRESS    btAddress;
            public int                  ulClassOfDevice;
            public int                  fConnected;
            public int                  fRemembered;
            public int                  fAuthenticated;
            public SYSTEMTIME           stLastSeen;
            public SYSTEMTIME           stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=WIN32.BLUETOOTH_MAX_NAME_SIZE)] public string szName;

            public void Initialize()
                {
                this.dwSize = Marshal.SizeOf(this);
                }

            public int ClassOfDeviceMajor { get { return (ulClassOfDevice >> 8) & 0x1F; }}
            public int ClassOfDeviceMinor { get { return (ulClassOfDevice >> 2) & 0x2F; }}
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
            public int     dwSize;
            public int     fReturnAuthenticated;
            public int     fReturnRemembered;
            public int     fReturnUnknown;
            public int     fReturnConnected;
            public int     fIssueInquiry;
            public byte    cTimeoutMultiplier;
            public IntPtr  hRadio;

            public void Initialize()
                {
                this.dwSize = Marshal.SizeOf(this);
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct COMMTIMEOUTS
            {
            public int      ReadIntervalTimeout;
            public int      ReadTotalTimeoutMultiplier;
            public int      ReadTotalTimeoutConstant;
            public int      WriteTotalTimeoutMultiplier;
            public int      WriteTotalTimeoutConstant;
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DCB 
            {
            public int     DCBlength;
            public int     BaudRate;
            public uint    grfFlags;
            /*
            int fBinary             :1;
            int fParity             :1;
            int fOutxCtsFlow        :1;
            int fOutxDsrFlow        :1;
            int fDtrControl         :2;
            int fDsrSensitivity     :1;
            int fTXContinueOnXoff   :1;
            int fOutX               :1;
            int fInX                :1;
            int fErrorChar          :1;
            int fNull               :1;
            int fRtsControl         :2;
            int fAbortOnError       :1;
            int fDummy2             :17; */
            public short   wReserved;
            public short   XonLim;
            public short   XoffLim;
            public byte    ByteSize;
            public byte    Parity;
            public byte    StopBits;
            public byte    XonChar;
            public byte    XoffChar;
            public byte    ErrorChar;
            public byte    EofChar;
            public byte    EvtChar;
            public short   wReserved1;

            public void Initialize()
                {
                this.DCBlength = Marshal.SizeOf(this);
                }

            public void SetFlag(DCBFLAG flag, int value)
                {
                uint mask;
                int cbitShift = (int)flag;
                value = value << cbitShift;
                if ((flag == DCBFLAG.DTRCONTROL) || (flag == DCBFLAG.RTSCONTROL))
                    {
                    mask = 3;
                    }
                else if (flag == DCBFLAG.DUMMY)
                    {
                    mask = 0x1ffff;
                    }
                else
                    {
                    mask = 1;
                    }
                this.grfFlags &= ~(mask << cbitShift);
                this.grfFlags |= (uint)value;
                }
            }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public class DEV_BROADCAST_DEVICEINTERFACE
            {
            public int      dbcc_size;
            public int      dbcc_devicetype;
            public int      dbcc_reserved;
            public Guid     dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string   dbcc_name;
            }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public class DEV_BROADCAST_HDR
            {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVICE_INTERFACE_DATA
            {
            public int          cbSize;
            public System.Guid  InterfaceClassGuid;
            public int          Flags;
            public IntPtr       Reserved;

            public void Initialize()
                {
                this.cbSize = Marshal.SizeOf(this);
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
            {
            public int          cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public String       DevicePath;

            public void Initialize()
                {
                // Odd convention used for this size
                this.cbSize = Marshal.OffsetOf(this.GetType(), "DevicePath").ToInt32() + Marshal.SystemDefaultCharSize;
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SP_DEVINFO_DATA
            {
            public int          cbSize;
            public System.Guid  ClassGuid;
            public int          DevInst;
            public int          Reserved;
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct SYSTEMTIME
            {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
            }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_CONFIGURATION_DESCRIPTOR
            {
            public Byte bLength;
            public Byte bDescriptorType;
            public ushort wTotalLength;
            public Byte bNumInterfaces;
            public Byte bConfigurationValue;
            public Byte iConfiguration;
            public Byte bmAttributes;
            public Byte MaxPower;
            }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_INTERFACE_DESCRIPTOR
            {
            public Byte bLength;
            public Byte bDescriptorType;
            public Byte bInterfaceNumber;
            public Byte bAlternateSetting;
            public Byte bNumEndpoints;
            public Byte bInterfaceClass;
            public Byte bInterfaceSubClass;
            public Byte bInterfaceProtocol;
            public Byte iInterface;
            }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINUSB_PIPE_INFORMATION
            {
            public USBD_PIPE_TYPE PipeType;
            public Byte PipeId;
            public ushort MaximumPacketSize;
            public Byte Interval;
            }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WINUSB_SETUP_PACKET
            {
            public Byte RequestType;
            public Byte Request;
            public ushort Value;
            public ushort Index;
            public ushort Length;
            }


        //------------------------------------------------------------------------------
        // Constants
        //------------------------------------------------------------------------------

        public static Guid   IID_IUnknown         = new Guid("00000000-0000-0000-C000-000000000046");
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int BLUETOOTH_MAX_NAME_SIZE             = (248);
        public const int BLUETOOTH_MAX_PASSKEY_SIZE          = (16);
        public const int BLUETOOTH_MAX_PASSKEY_BUFFER_SIZE   = (BLUETOOTH_MAX_PASSKEY_SIZE + 1);
        public const int BLUETOOTH_MAX_SERVICE_NAME_SIZE     = (256);
        public const int BLUETOOTH_DEVICE_NAME_SIZE          = (256);

        public const int DIGCF_PRESENT                       = 2;
        public const int DIGCF_DEVICEINTERFACE               = 0X10;

        public const int DBT_DEVICEARRIVAL                   = 0X8000;
        public const int DBT_DEVICEREMOVECOMPLETE            = 0X8004;
        public const int DBT_DEVTYP_DEVICEINTERFACE          = 5;
        public const int DBT_DEVTYP_HANDLE                   = 6;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        public const int DEVICE_NOTIFY_SERVICE_HANDLE        = 1;
        public const int DEVICE_NOTIFY_WINDOW_HANDLE         = 0;

        public const int  MAX_PATH = 260;

        public const int  FILE_ATTRIBUTE_NORMAL       = 0X80;
        public const int  FILE_FLAG_OVERLAPPED        = 0X40000000;
        public const int  FILE_SHARE_READ             = 1;
        public const int  FILE_SHARE_WRITE            = 2;
        public const uint GENERIC_READ                = 0X80000000;
        public const uint GENERIC_WRITE               = 0X40000000;
        public const int  OPEN_EXISTING               = 3;

        public const uint DEVICE_SPEED                = ((uint)(1));
        public const byte USB_ENDPOINT_DIRECTION_MASK = ((byte)(0X80));

        public const int WM_DEVICECHANGE                     = 0X219;

        //------------------------------------------------------------------------------
        // Enums
        //------------------------------------------------------------------------------

        public enum BIND : int
            {
            MAYBOTHERUSER       = 1,
            JUSTTESTEXISTENCE   = 2 
            }

        public enum CLSCTX : uint
            {
            INPROC_SERVER       = 0x1,
            INPROC_HANDLER      = 0x2,
            LOCAL_SERVER        = 0x4,
            INPROC_SERVER16     = 0x8,
            REMOTE_SERVER       = 0x10,
            INPROC_HANDLER16    = 0x20,
            RESERVED1           = 0x40,
            RESERVED2           = 0x80,
            RESERVED3           = 0x100,
            RESERVED4           = 0x200,
            NO_CODE_DOWNLOAD    = 0x400,
            RESERVED5           = 0x800,
            NO_CUSTOM_MARSHAL   = 0x1000,
            ENABLE_CODE_DOWNLOAD    = 0x2000,
            NO_FAILURE_LOG          = 0x4000,
            DISABLE_AAA             = 0x8000,
            ENABLE_AAA              = 0x10000,
            FROM_DEFAULT_CONTEXT    = 0x20000,
            ACTIVATE_32_BIT_SERVER  = 0x40000,
            ACTIVATE_64_BIT_SERVER  = 0x80000,
            ENABLE_CLOAKING         = 0x100000,
            PS_DLL                  = 0x80000000,
            }

        public enum DCBFLAG : int
            {
            BINARY          = 0,
            PARITY          = 1,
            OUTXCTXFLOW     = 2,
            OUTXDSRFLOW     = 3,
            DTRCONTROL      = 4,
            DSRSENSITIVITY  = 6,
            TXCONTINUEONXOFF = 7,
            OUTX            = 8,
            INX             = 9,
            ERRORCHAR       = 10,
            DISCARDNULL     = 11,
            RTSCONTROL      = 12,
            ABORTONERROR    = 14,
            DUMMY           = 15
            }

        public enum OLEGETMONIKER : uint
            {
            ONLYIFTHERE = 1,
            FORCEASSIGN = 2,
            UNASSIGN    = 3,
            TEMPFORUSER = 4
            };

        public enum OLEWHICHMK : uint
            {
            CONTAINER = 1,
            OBJREL    = 2,
            OBJFULL   = 3
            };

        public enum OLEMISC : uint
            {
            RECOMPOSEONRESIZE           = 0x00000001,
            ONLYICONIC                  = 0x00000002,
            INSERTNOTREPLACE            = 0x00000004,
            STATIC                      = 0x00000008,
            CANTLINKINSIDE              = 0x00000010,
            CANLINKBYOLE1               = 0x00000020,
            ISLINKOBJECT                = 0x00000040,
            INSIDEOUT                   = 0x00000080,
            ACTIVATEWHENVISIBLE         = 0x00000100,
            RENDERINGISDEVICEINDEPENDENT= 0x00000200,
            INVISIBLEATRUNTIME          = 0x00000400,
            ALWAYSRUN                   = 0x00000800,
            ACTSLIKEBUTTON              = 0x00001000,
            ACTSLIKELABEL               = 0x00002000,
            NOUIACTIVATE                = 0x00004000,
            ALIGNABLE                   = 0x00008000,
            SIMPLEFRAME                 = 0x00010000,
            SETCLIENTSITEFIRST          = 0x00020000,
            IMEMODE                     = 0x00040000,
            IGNOREACTIVATEWHENVISIBLE   = 0x00080000,
            WANTSTOMENUMERGE            = 0x00100000,
            SUPPORTSMULTILEVELUNDO      = 0x00200000
            };

        public enum OLECLOSE : uint
            {
            SAVEIFDIRTY = 0,
            NOSAVE      = 1,
            PROMPTSAVE  = 2
            };

        public enum OLEIVERB : int
            {
            PRIMARY          =  0,
            SHOW             = -1,
            OPEN             = -2,
            HIDE             = -3,
            UIACTIVATE       = -4,
            INPLACEACTIVATE  = -5,
            DISCARDUNDOSTATE = -6,
            };

        public enum POLICY_TYPE
            {
            SHORT_PACKET_TERMINATE = 1,
            AUTO_CLEAR_STALL,
            PIPE_TRANSFER_TIMEOUT,
            IGNORE_SHORT_PACKETS,
            ALLOW_PARTIAL_READS,
            AUTO_FLUSH,
            RAW_IO,
            }

        public enum USBD_PIPE_TYPE
            {
            UsbdPipeTypeControl,
            UsbdPipeTypeIsochronous,
            UsbdPipeTypeBulk,
            UsbdPipeTypeInterrupt,
            }

        public enum USB_DEVICE_SPEED
            {
            UsbLowSpeed = 1,
            UsbFullSpeed,
            UsbHighSpeed,
            }

        public enum USERCLASSTYPE : uint
            {
            FULL    = 1,
            SHORT   = 2,
            APPNAME = 3,
            };

        //------------------------------------------------------------------------------
        // APIs
        //------------------------------------------------------------------------------

        [DllImport("bthprops.cpl", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool BluetoothFindDeviceClose(IntPtr hFind);

        [DllImport("bthprops.cpl", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS pSearchParams, ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool BluetoothFindNextDevice(IntPtr hFind, ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool CancelIo(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool CloseHandle(IntPtr handle);

        [DllImport("ole32.dll")] public static extern
        int CoCreateInstance(
            [In] ref Guid rclsid,
            [In, MarshalAs(UnmanagedType.IUnknown)] Object pUnkOuter,
            [In, MarshalAs(UnmanagedType.U4)] CLSCTX dwClsContext,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out Object ppv);

        [DllImport("ole32.dll")] public static extern 
        int CreateBindCtx(
            [In] uint reserved,
            [Out, MarshalAs(UnmanagedType.Interface)] out COMTypes.IBindCtx pbindContext);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="CreateFileW")] public static extern 
        SafeFileHandle SafeCreateFile(String lpFileName, 
            uint    dwDesiredAccess, 
            int     dwShareMode, 
            IntPtr  lpSecurityAttributes, 
            int     dwCreationDisposition, 
            int     dwFlagsAndAttributes, 
            IntPtr  hTemplateFile);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="CreateFileW")] public static extern 
        IntPtr CreateFile(String lpFileName, 
            uint    dwDesiredAccess, 
            int     dwShareMode, 
            IntPtr  lpSecurityAttributes, 
            int     dwCreationDisposition, 
            int     dwFlagsAndAttributes, 
            IntPtr  hTemplateFile);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool GetCommState(IntPtr hFile, ref DCB dcb);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool GetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS timeouts);

        [DllImport("kernel32.dll", SetLastError=true)] public static extern 
        IntPtr GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string moduleName);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool GetOverlappedResult(IntPtr hFile, IntPtr pOverlapped, ref int cbTransferred, int fWait);

        [DllImport("ole32.dll")] public static extern 
        int MkParseDisplayName(
            [In, MarshalAs(UnmanagedType.Interface)]      COMTypes.IBindCtx pbindContext,
            [In, MarshalAs(UnmanagedType.LPWStr)]         string            sDisplayName,
                                                          IntPtr            pcchEaten,
            [Out, MarshalAs(UnmanagedType.Interface)] out COMTypes.IMoniker ppmk);

        [DllImport("ole32.dll")] public static extern int OleInitialize(IntPtr reserved);
        [DllImport("ole32.dll")] public static extern int OleUninitialize();

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool ReadFile(IntPtr hFile, byte[] rgbBuffer, int cbToRead, out int cbRead, IntPtr overlapped);

        [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError = true, EntryPoint="RegisterDeviceNotificationW")] public static extern 
        IntPtr RegisterDeviceNotification(IntPtr hRecipient, ref DEV_BROADCAST_HDR filter, int Flags);

        [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError = true, EntryPoint="RegisterDeviceNotificationW")] public static extern 
        IntPtr RegisterDeviceNotification(IntPtr hRecipient, DEV_BROADCAST_DEVICEINTERFACE filter, int Flags);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool SetCommState(IntPtr hFile, ref DCB dcb);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS timeouts);


        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        IntPtr SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, IntPtr hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiDestroyDeviceInfoList(IntPtr hDeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, out SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern 
        IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern 
        bool SetupDiGetDeviceInterfaceDetail(IntPtr hDeviceInfoSet, 
            ref SP_DEVICE_INTERFACE_DATA        DeviceInterfaceData, 
            ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, 
            int DeviceInterfaceDetailDataSize, 
            out int cbRequired, 
            IntPtr DeviceInfoData);

        [DllImport("user32.dll", SetLastError = true)] public static extern 
        bool UnregisterDeviceNotification(IntPtr Handle);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_AbortPipe(IntPtr hInterface, byte PipeID);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_ControlTransfer(IntPtr hInterface, WINUSB_SETUP_PACKET SetupPacket, byte[] Buffer, uint BufferLength, ref uint LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_FlushPipe(IntPtr hInterface, byte PipeID);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_Free(IntPtr hInterface);

        // WinUsb_GetAssociatedInterface
        // WinUsb_GetCurrentAlternateSetting
        // WinUsb_GetDescriptor

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_GetOverlappedResult(IntPtr hInterface, IntPtr pOverlapped, ref int cbTransfered, int fWait);

        // WinUsb_GetPipePolicy
        // WinUsb_GetPowerPolicy

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_Initialize(SafeFileHandle hDevice, ref IntPtr hInterface);

        //  Use this declaration to retrieve DEVICE_SPEED (the only currently defined InformationType).
        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_QueryDeviceInformation(IntPtr hInterface, uint InformationType, ref uint BufferLength, ref byte Buffer);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_QueryInterfaceSettings(IntPtr hInterface, byte AlternateInterfaceNumber, ref USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport("winusb.dll", SetLastError=true)] public static extern unsafe
        bool WinUsb_QueryPipe(IntPtr hInterface, byte AlternateInterfaceNumber, byte PipeIndex, ref WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_ReadPipe(IntPtr hInterface, byte PipeID, byte[] rgbBuffer, int cbBuffer, out int cbRead, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_ResetPipe(IntPtr hInterface, byte PipeID);

        // WinUsb_SetCurrentAlternateSetting

        //  Two declarations for WinUsb_SetPipePolicy. 
        //  Use this one when the returned Value is a byte (all except PIPE_TRANSFER_TIMEOUT):
        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_SetPipePolicy(IntPtr hInterface, byte PipeID, uint PolicyType, int ValueLength, ref byte Value);

        //  Use this alias when the returned Value is a uint (PIPE_TRANSFER_TIMEOUT only):
        [DllImport("winusb.dll", SetLastError=true, EntryPoint = "WinUsb_SetPipePolicy")] public static extern 
        bool WinUsb_SetPipePolicy1(IntPtr hInterface, byte PipeID, uint PolicyType, int ValueLength, ref int Value);

        // WinUsb_SetPowerPolicy

        [DllImport("winusb.dll", SetLastError=true)] public static extern 
        bool WinUsb_WritePipe(IntPtr hInterface, byte PipeID, byte[] Buffer, int BufferLength, ref int LengthTransferred, IntPtr Overlapped);

        [DllImport("kernel32.dll", SetLastError=true)] public static extern 
        bool WriteFile(IntPtr hFile, byte[] rgbBuffer, int cbToWrite, out int cbWritten, IntPtr overlapped);

        //-------------------------------------------------------------------------

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
        public static void ThrowWin32Error()
            {
            ThrowWin32Error(Marshal.GetLastWin32Error());
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

        //------------------------------------------------------------------------------
        // Interfaces
        //------------------------------------------------------------------------------

        [ComImport, Guid("00000112-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleObject
        // Not yet complete here, just enough now for what we currently need
            {
            void _SetClientSite();
            void _GetClientSite();
            void _SetHostNames();
            void Close(WIN32.OLECLOSE dwSaveOption);
            void _SetMoniker();
            void _GetMoniker();
            void _InitFromData();
            void _GetClipboardData();
            void DoVerb(
                WIN32.OLEIVERB iVerb,
                IntPtr lpmsg,           // zero if you don't have one
                [MarshalAs(UnmanagedType.Interface)] IOleClientSite pActiveSite,
                int lindex,             // reserved, must be zero
                IntPtr hwnd,
                IntPtr lprcPosRect
                );
            void _EnumVerbs();
            void Update();
            [PreserveSig] int IsUpToDate();
            void _GetUserClassID();
            void _GetUserType();
            void _SetExtent();
            void _GetExtent();
            void _Advise();
            void _Unadvise();
            void _EnumAdvise();
            void _GetMiscStatus();
            void _SetColorScheme();
            }

        [ComImport, Guid("00000118-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleClientSite
            {
            void SaveObject();
            void GetMoniker(uint dwAssign, uint dwWhichMoniker, [Out,MarshalAs(UnmanagedType.Interface)] out COMTypes.IMoniker ppmk);
            void GetContainer([Out,MarshalAs(UnmanagedType.Interface)] out IOleContainer ppContainer);
            void ShowObject();
            void OnShowWindow([MarshalAs(UnmanagedType.I4)] bool fShow);
            void RequestNewObjectLayout();
            }

        [ComImport, Guid("0000011a-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IParseDisplayName
            {
            void ParseDisplayName(
                [MarshalAs(UnmanagedType.Interface)] COMTypes.IBindCtx pbc,
                [MarshalAs(UnmanagedType.LPWStr)] string sDisplayName,
                IntPtr pcchEaten,
                [Out,MarshalAs(UnmanagedType.Interface)] out COMTypes.IMoniker ppmk);
            }

        [ComImport, Guid("0000011b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleContainer // : IParseDisplayName
            {
            void ParseDisplayName(
                [MarshalAs(UnmanagedType.Interface)] COMTypes.IBindCtx pbc,
                [MarshalAs(UnmanagedType.LPWStr)] string sDisplayName,
                IntPtr pcchEaten,
                [Out,MarshalAs(UnmanagedType.Interface)] out COMTypes.IMoniker ppmk);

            void EnumObjects(uint grfFlags, [Out,MarshalAs(UnmanagedType.Interface)] out object ppenum); // actually IEnumUnknown, not object
            void LockContainer([MarshalAs(UnmanagedType.I4)] bool fLock);
            }

        //------------------------------------------------------------------------------
        // Helpers
        //------------------------------------------------------------------------------

        public static COMTypes.IBindCtx CreateBindContext()
            {
            COMTypes.IBindCtx pbc;
            int hr = WIN32.CreateBindCtx(0, out pbc);
            if (0==hr)
                {
                COMTypes.BIND_OPTS opts = new COMTypes.BIND_OPTS();
                opts.cbStruct = Marshal.SizeOf(opts);
                opts.grfMode = (int)BIND.MAYBOTHERUSER;
                pbc.SetBindOptions(ref opts);
                return pbc;
                }
            else
                {
                    Marshal.ThrowExceptionForHR(hr);
                return null;
                }
            }

        public static COMTypes.IMoniker MkParseDisplayName(string sDisplayName)
            {
            COMTypes.IMoniker ppmk = null;
            COMTypes.IBindCtx pbc = WIN32.CreateBindContext();
            IntPtr cchEaten = Marshal.AllocCoTaskMem(sizeof(int));
            try {
                int hr = MkParseDisplayName(pbc, sDisplayName, cchEaten, out ppmk);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);
                }
            finally
                {
                Marshal.FreeCoTaskMem(cchEaten);
                }

            return ppmk;
            }

        public static COMTypes.IMoniker LoadMoniker(byte[] rgb)
        // Load a moniker from its serialized representation
            {
            // The moniker's clsid are the first bytes of the data
            //
            byte[] rgbClsid   = new byte[16];
            byte[] rgbMoniker = new byte[rgb.Length - 16];
            Array.ConstrainedCopy(rgb, 0,  rgbClsid,   0, 16);
            Array.ConstrainedCopy(rgb, 16, rgbMoniker, 0, rgb.Length - 16);
            Guid clsid = new Guid(rgbClsid);
            //
            // Instantiate an instance of that moniker class
            //
            object punk;
            int hr = WIN32.CoCreateInstance(ref clsid, null, WIN32.CLSCTX.INPROC_SERVER, ref WIN32.IID_IUnknown, out punk);
            if (0==hr)
                {
                COMTypes.IMoniker pmk = (COMTypes.IMoniker)punk;
                //
                // Load the moniker from the data
                //
                System.IO.MemoryStream memstm = new System.IO.MemoryStream(rgbMoniker);
                COMTypes.IStream istm = new COMStreamOnSystemIOStream(memstm);
                pmk.Load(istm);
                return pmk;
                }
            else
                {
                Marshal.ThrowExceptionForHR(hr);
                return null;
                }
            }

        public static Guid IIDOf(Type type)
            {
            foreach (object o in type.GetCustomAttributes(false))
                {
                GuidAttribute guidAttr = o as GuidAttribute;
                if (guidAttr != null)
                    {
                    return new Guid(guidAttr.Value);
                    }
                }
            throw new InvalidOperationException();
            }

        public class OleClientSite : WIN32.IOleClientSite
            {
            #region IOleClientSite Members

            void WIN32.IOleClientSite.SaveObject()
                {
                throw new NotImplementedException();
                }

            void WIN32.IOleClientSite.GetMoniker(uint dwAssign, uint dwWhichMoniker, out COMTypes.IMoniker ppmk)
                {
                throw new NotImplementedException();
                }

            void WIN32.IOleClientSite.GetContainer(out WIN32.IOleContainer ppContainer)
                {
                throw new NotImplementedException();
                }

            void WIN32.IOleClientSite.ShowObject()
                {
                throw new NotImplementedException();
                }

            void WIN32.IOleClientSite.OnShowWindow(bool fShow)
                {
                throw new NotImplementedException();
                }

            void WIN32.IOleClientSite.RequestNewObjectLayout()
                {
                throw new NotImplementedException();
                }

            #endregion
            }

        public static byte[] GetData(IDataObject oData, string cfFormat)
        // Get this clipboard format out of this data object
            {
            // First, we try using the built-in functionality. Unfortunately, in the TYMED_ISTREAM case
            // they forget to seek the stream to zero, and so aren't successful. We'll take care of that
            // in a moment.
            //
            System.IO.Stream stm = (System.IO.Stream)oData.GetData(cfFormat, false);
            if (null != stm)
                {
                stm.Seek(0, System.IO.SeekOrigin.Begin);
                int    cb  = (int)stm.Length;
                byte[] rgb = new byte[cb];
                int cbRead = stm.Read(rgb, 0, cb);
                if (cbRead == cb)
                    {
                    // The bug is that the data returned is entirely zero. Let's check.
                    //
                    for (int ib=0; ib < cb; ib++)
                        {
                        if (rgb[ib] != 0)
                            return rgb;
                        }
                    }
                }
            //
            // Ok, try to see if we can get it on a stream ourselves
            //
            COMTypes.IDataObject ido       = (COMTypes.IDataObject)oData;
            COMTypes.FORMATETC   formatEtc = new COMTypes.FORMATETC();
            COMTypes.STGMEDIUM   medium    = new COMTypes.STGMEDIUM();
            formatEtc.cfFormat = (short)DataFormats.GetFormat(cfFormat).Id;
            formatEtc.dwAspect = COMTypes.DVASPECT.DVASPECT_CONTENT;
            formatEtc.lindex   = -1;                            // REVIEW: is 0 better?
            formatEtc.tymed    = COMTypes.TYMED.TYMED_ISTREAM;
            //
            ido.GetData(ref formatEtc, out medium);
            //
            if (medium.unionmember != IntPtr.Zero)
                {
                // Get at the IStream and release the ref in the medium
                COMTypes.IStream istm = (COMTypes.IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                Marshal.Release(medium.unionmember);

                // How big is the stream?
                COMTypes.STATSTG statstg = new COMTypes.STATSTG();
                istm.Stat(out statstg, 1);
                int cb = (int)statstg.cbSize;
                byte[] rgb = new byte[cb];

                // Seek the stream to the beginning and read the data
                IntPtr pcbRead = Marshal.AllocCoTaskMem(sizeof(int));
                istm.Seek(0, 0, IntPtr.Zero);
                istm.Read(rgb, cb, pcbRead);
                int cbRead = Marshal.ReadInt32(pcbRead);
                Marshal.FreeCoTaskMem(pcbRead);

                if (cb==cbRead)
                    return rgb;
                }
            //
            // Can't get the data
            //
            return null;
            }

        }

    //=================================================================================

    // Implementation of COM's IStream on a .NET stream
    // Incomplete, just enough for what we currently need.
    public class COMStreamOnSystemIOStream : COMTypes.IStream
        {
        //----------------------------------
        // State
        //----------------------------------

        System.IO.Stream stm;

        //----------------------------------
        // Construction
        //----------------------------------

        public COMStreamOnSystemIOStream(System.IO.Stream stm)
            {
            this.stm = stm;
            }

        //----------------------------------
        // IStream
        //----------------------------------

        public void Clone(out COMTypes.IStream ppstm)
            {
            throw new NotImplementedException();
            }

        public void Commit(int grfCommitFlags)
            {
            // Nothing to do
            }

        public void CopyTo(COMTypes.IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
            throw new NotImplementedException();
            }

        public void LockRegion(long libOffset, long cb, int dwLockType)
            {
            Marshal.ThrowExceptionForHR(WIN32.STG_E_INVALIDFUNCTION);
            }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
            {
            int cbRead = this.stm.Read(pv, 0, cb);
            Marshal.WriteInt32(pcbRead, cbRead);
            }

        public void Revert()
            {
            // Nothing to do
            }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
            long ibNew = this.stm.Seek(dlibMove, (System.IO.SeekOrigin)dwOrigin);
            Marshal.WriteInt64(plibNewPosition, ibNew);
            }

        public void SetSize(long libNewSize)
            {
            this.stm.SetLength(libNewSize);
            }

        public void Stat(out COMTypes.STATSTG pstatstg, int grfStatFlag)
            {
            pstatstg = new COMTypes.STATSTG();
            pstatstg.cbSize = this.stm.Length;
            }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
            throw new NotImplementedException();
            }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
            {
            throw new NotImplementedException();
            }
        }
    }

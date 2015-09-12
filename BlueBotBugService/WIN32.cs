//
// WIN32.cs
//
// Plumbing for interacting with native code Windows APIs. Some of this
// is rocket science, and it all certainly could be documented better.

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using COMTypes = System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32.SafeHandles;

namespace Org.SwerveRobotics.BotBug.Service
    {
    //=================================================================================

    public static class WIN32
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
        public const int ERROR_NO_MORE_ITEMS            = 259;
        public const int ERROR_OPERATION_ABORTED        = 995;
        public const int ERROR_IO_INCOMPLETE            = 996;
        public const int ERROR_IO_PENDING               = 997;
        public const int ERROR_EXCEPTION_IN_SERVICE     = 1064;
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
        // USBIODef.h
        //------------------------------------------------------------------------------

        public static Guid GUID_DEVINTERFACE_USB_HUB                = new Guid("f18a0e88-c30c-11d0-8815-00a0c906bed8");
        public static Guid GUID_DEVINTERFACE_USB_DEVICE             = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
        public static Guid GUID_DEVINTERFACE_USB_HOST_CONTROLLER    = new Guid("3ABF6F2D-71C4-462a-8A92-1E6861E6AF27");
        public static Guid GUID_USB_WMI_STD_DATA                    = new Guid("4E623B20-CB14-11D1-B331-00A0C959BBD2");
        public static Guid GUID_USB_WMI_STD_NOTIFICATION            = new Guid("4E623B20-CB14-11D1-B331-00A0C959BBD2");

        //------------------------------------------------------------------------------
        // Dbt.h
        //------------------------------------------------------------------------------

        /*
         * BroadcastSpecialMessage constants.
         */
        public const int WM_DEVICECHANGE = 0x0219;
  
        /*
         * Broadcast message and receipient flags.
         *
         * Note that there is a third "flag". If the wParam has:
         *
         * bit 15 on:   lparam is a pointer and bit 14 is meaningfull.
         * bit 15 off:  lparam is just a UNLONG data type.
         *
         * bit 14 on:   lparam is a pointer to an ASCIIZ string.
         * bit 14 off:  lparam is a pointer to a binary struture starting with
         *              a dword describing the length of the structure.
         */
        public const int BSF_QUERY = 0x00000001;
        public const int BSF_IGNORECURRENTTASK = 0x00000002;    /* Meaningless for VxDs */
        public const int BSF_FLUSHDISK = 0x00000004;            /* Shouldn't be used by VxDs */
        public const int BSF_NOHANG = 0x00000008;
        public const int BSF_POSTMESSAGE = 0x00000010;
        public const int BSF_FORCEIFHUNG = 0x00000020;
        public const int BSF_NOTIMEOUTIFNOTHUNG = 0x00000040;
        public const uint BSF_MSGSRV32ISOK = 0x80000000;        /* Called synchronously from PM API */
        public const int BSF_MSGSRV32ISOK_BIT = 31;             /* Called synchronously from PM API */

        public const int BSM_ALLCOMPONENTS = 0x00000000;
        public const int BSM_VXDS = 0x00000001;
        public const int BSM_NETDRIVER = 0x00000002;
        public const int BSM_INSTALLABLEDRIVERS = 0x00000004;
        public const int BSM_APPLICATIONS = 0x00000008;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_APPYBEGIN
         * lParam  = (not used)
         *
         *      'Appy-time is now available.  This message is itself sent
         *      at 'Appy-time.
         *
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_APPYEND
         * lParam  = (not used)
         *
         *      'Appy-time is no longer available.  This message is *NOT* sent
         *      at 'Appy-time.  (It cannot be, because 'Appy-time is gone.)
         *
         * NOTE!  It is possible for DBT_APPYBEGIN and DBT_APPYEND to be sent
         * multiple times during a single Windows session.  Each appearance of
         * 'Appy-time is bracketed by these two messages, but 'Appy-time may
         * momentarily become unavailable during otherwise normal Windows
         * processing.  The current status of 'Appy-time availability can always
         * be obtained from a call to _SHELL_QueryAppyTimeAvailable.
         */
        public const int DBT_APPYBEGIN = 0x0000;
        public const int DBT_APPYEND = 0x0001;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_DEVNODES_CHANGED
         * lParam  = 0
         *
         *      send when configmg finished a process tree batch. Some devnodes
         *      may have been added or removed. This is used by ring3 people which
         *      need to be refreshed whenever any devnode changed occur (like
         *      device manager). People specific to certain devices should use
         *      DBT_DEVICE* instead.
         */

        public const int DBT_DEVNODES_CHANGED = 0x0007;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_QUERYCHANGECONFIG
         * lParam  = 0
         *
         *      sent to ask if a config change is allowed
         */

        public const int DBT_QUERYCHANGECONFIG = 0x0017;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_CONFIGCHANGED
         * lParam  = 0
         *
         *      sent when a config has changed
         */

        public const int DBT_CONFIGCHANGED = 0x0018;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_CONFIGCHANGECANCELED
         * lParam  = 0
         *
         *      someone cancelled the config change
         */

        public const int DBT_CONFIGCHANGECANCELED = 0x0019;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_MONITORCHANGE
         * lParam  = new resolution to use (LOWORD=x, HIWORD=y)
         *           if 0, use the default res for current config
         *
         *      this message is sent when the display monitor has changed
         *      and the system should change the display mode to match it.
         */

        public const int DBT_MONITORCHANGE = 0x001B;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_SHELLLOGGEDON
         * lParam  = 0
         *
         *      The shell has finished login on: VxD can now do Shell_EXEC.
         */

        public const int DBT_SHELLLOGGEDON = 0x0020;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_CONFIGMGAPI
         * lParam  = CONFIGMG API Packet
         *
         *      CONFIGMG ring 3 call.
         */
        public const int DBT_CONFIGMGAPI32 = 0x0022;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_VXDINITCOMPLETE
         * lParam  = 0
         *
         *      CONFIGMG ring 3 call.
         */
        public const int DBT_VXDINITCOMPLETE = 0x0023;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_VOLLOCK*
         * lParam  = pointer to VolLockBroadcast structure described below
         *
         *      Messages issued by IFSMGR for volume locking purposes on WM_DEVICECHANGE.
         *      All these messages pass a pointer to a struct which has no pointers.
         */

        public const int DBT_VOLLOCKQUERYLOCK = 0x8041;
        public const int DBT_VOLLOCKLOCKTAKEN = 0x8042;
        public const int DBT_VOLLOCKLOCKFAILED = 0x8043;
        public const int DBT_VOLLOCKQUERYUNLOCK = 0x8044;
        public const int DBT_VOLLOCKLOCKRELEASED = 0x8045;
        public const int DBT_VOLLOCKUNLOCKFAILED = 0x8046;

        /*
         * Device broadcast header
         */

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_HDR 
            {     /* */
            public int  dbch_size;
            public int  dbch_devicetype;
            public int  dbch_reserved;

            public string DeviceTypeName { get {
                switch (this.dbch_devicetype)
                    {
                case DBT_DEVTYP_OEM:             return "DBT_DEVTYP_OEM";
                case DBT_DEVTYP_DEVNODE:         return "DBT_DEVTYP_DEVNODE";
                case DBT_DEVTYP_VOLUME:          return "DBT_DEVTYP_VOLUME";
                case DBT_DEVTYP_PORT:            return "DBT_DEVTYP_PORT";
                case DBT_DEVTYP_NET:             return "DBT_DEVTYP_NET";
                case DBT_DEVTYP_DEVICEINTERFACE: return "DBT_DEVTYP_DEVICEINTERFACE";
                case DBT_DEVTYP_HANDLE:          return "DBT_DEVTYP_HANDLE";
                    }
                return String.Format("unknownType({0})", this.dbch_devicetype);
                } }
            };

        /*
         * Structure for volume lock broadcast
         */

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct VolLockBroadcast
                {
                public DEV_BROADCAST_HDR vlb_dbh;
                public int     vlb_owner;              // thread on which lock request is being issued
                public byte    vlb_perms;              // lock permission flags defined below
                public byte    vlb_lockType;           // type of lock
                public byte    vlb_drive;              // drive on which lock is issued
                public byte    vlb_flags;              // miscellaneous flags
                };

        /*
         * Values for vlb_perms
         */
        public const int LOCKP_ALLOW_WRITES = 0x01;    // Bit 0 set - allow writes
        public const int LOCKP_FAIL_WRITES = 0x00;    // Bit 0 clear - fail writes
        public const int LOCKP_FAIL_MEM_MAPPING = 0x02;    // Bit 1 set - fail memory mappings
        public const int LOCKP_ALLOW_MEM_MAPPING = 0x00;    // Bit 1 clear - allow memory mappings
        public const int LOCKP_USER_MASK = 0x03;    // Mask for user lock flags
        public const int LOCKP_LOCK_FOR_FORMAT = 0x04;    // Level 0 lock for format

        /*
         * Values for vlb_flags
         */
        public const int LOCKF_LOGICAL_LOCK = 0x00;    // Bit 0 clear - logical lock
        public const int LOCKF_PHYSICAL_LOCK = 0x01;    // Bit 0 set - physical lock

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_NODISKSPACE
         * lParam  = drive number of drive that is out of disk space (1-based)
         *
         * Message issued by IFS manager when it detects that a drive is run out of
         * free space.
         */

        public const int DBT_NO_DISK_SPACE = 0x0047;

        /*
         * Message = WM_DEVICECHANGE
         * wParam  = DBT_LOW_DISK_SPACE
         * lParam  = drive number of drive that is low on disk space (1-based)
         *
         * Message issued by VFAT when it detects that a drive it has mounted
         * has the remaning free space below a threshold specified by the
         * registry or by a disk space management application.
         * The broadcast is issued by VFAT ONLY when space is either allocated
         * or freed by VFAT.
         */

        public const int DBT_LOW_DISK_SPACE = 0x0048;

        public const int DBT_CONFIGMGPRIVATE = 0x7FFF;

        /*
         * The following messages are for WM_DEVICECHANGE. The immediate list
         * is for the wParam. ALL THESE MESSAGES PASS A POINTER TO A STRUCT
         * STARTING WITH A DWORD SIZE AND HAVING NO POINTER IN THE STRUCT.
         *
         */
        public const int DBT_DEVICEARRIVAL = 0x8000;                // system detected a new device
        public const int DBT_DEVICEQUERYREMOVE = 0x8001;            // wants to remove, may fail
        public const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;      // removal aborted
        public const int DBT_DEVICEREMOVEPENDING = 0x8003;          // about to remove, still avail.
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;         // device is gone
        public const int DBT_DEVICETYPESPECIFIC = 0x8005;           // type specific event

        public const int DBT_CUSTOMEVENT = 0x8006;                  // user-defined event

        public const int DBT_DEVTYP_OEM = 0x00000000;               // oem-defined device type
        public const int DBT_DEVTYP_DEVNODE = 0x00000001;           // devnode number
        public const int DBT_DEVTYP_VOLUME = 0x00000002;            // logical volume
        public const int DBT_DEVTYP_PORT = 0x00000003;              // serial, parallel
        public const int DBT_DEVTYP_NET = 0x00000004;               // network resource
        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;   // device interface class
        public const int DBT_DEVTYP_HANDLE = 0x00000006;            // file system handle


        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct _DEV_BROADCAST_HEADER 
            {
            public int       dbcd_size;
            public int       dbcd_devicetype;
            public int       dbcd_reserved;
            };

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_OEM 
            {
            public int       dbco_size;
            public int       dbco_devicetype;
            public int       dbco_reserved;
            public int       dbco_identifier;
            public int       dbco_suppfunc;
            };

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_DEVNODE 
            {
            public int       dbcd_size;
            public int       dbcd_devicetype;
            public int       dbcd_reserved;
            public int       dbcd_devnode;
            };

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_VOLUME 
            {
            public int       dbcv_size;
            public int       dbcv_devicetype;
            public int       dbcv_reserved;
            public int       dbcv_unitmask;
            public short     dbcv_flags;
            };

        public const int DBTF_MEDIA = 0x0001;          // media comings and goings
        public const int DBTF_NET = 0x0002;          // network volume

        // TODO: Define accessor method for the name
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
        public unsafe struct DEV_BROADCAST_PORT_A 
            {
            public int       dbcp_size;
            public int       dbcp_devicetype;
            public int       dbcp_reserved;
        //  char             dbcp_name[1];
            };

        // TODO: Define accessor method for the name
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public unsafe struct DEV_BROADCAST_PORT_W 
            {
            public int       dbcp_size;
            public int       dbcp_devicetype;
            public int       dbcp_reserved;
        //  wchar_t          dbcp_name[1];
            };

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_NET 
            {
            public int       dbcn_size;
            public int       dbcn_devicetype;
            public int       dbcn_reserved;
            public int       dbcn_resource;
            public int       dbcn_flags;
            };

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public class DEV_BROADCAST_DEVICEINTERFACE_MANAGED
        // Use this one for structures initialized in managed code; we *marshal* to get to non managed
            {
            public int      dbcc_size;
            public int      dbcc_devicetype;
            public int      dbcc_reserved;
            public Guid     dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string   dbcc_name;

            public void Initialize(Guid classGuid)
                {
                this.dbcc_size       = Marshal.SizeOf(this.GetType());
                this.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
                this.dbcc_reserved   = 0;
                this.dbcc_classguid  = classGuid;
                this.dbcc_name       = "";
                }
            }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
        public struct DEV_BROADCAST_DEVICEINTERFACE_A
            {
            public int          dbcc_size;
            public int          dbcc_devicetype;
            public int          dbcc_reserved;
            public System.Guid  dbcc_classguid;
        //  char                dbcc_name[1];

            public unsafe String dbcc_name { get 
                { 
                return Util.ToStringAnsi(this.PbVariablePart, this.PbMax - this.PbVariablePart);
                } }

            public unsafe byte* PbVariablePart { get { fixed(DEV_BROADCAST_DEVICEINTERFACE_A* pThis = &this) 
                { 
                return (byte*)(&pThis->dbcc_classguid) + sizeof(System.Guid); 
                } } }
            public unsafe byte* PbMax { get { fixed(DEV_BROADCAST_DEVICEINTERFACE_A* pThis = &this) 
                {
                return (byte*)pThis + pThis->dbcc_size;
                } } }
            };

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_DEVICEINTERFACE_W 
            {
            public int          dbcc_size;
            public int          dbcc_devicetype;
            public int          dbcc_reserved;
            public System.Guid  dbcc_classguid;
        //  wchar_t             dbcc_name[1];

            public unsafe String dbcc_name { get 
                { 
                return Util.ToStringUni(this.PbVariablePart, this.PbMax - this.PbVariablePart);
                } }

            public unsafe byte* PbVariablePart { get { fixed(DEV_BROADCAST_DEVICEINTERFACE_W* pThis = &this) 
                { 
                return (byte*)(&pThis->dbcc_classguid) + sizeof(System.Guid); 
                } } }
            public unsafe byte* PbMax { get { fixed(DEV_BROADCAST_DEVICEINTERFACE_W* pThis = &this) 
                {
                return (byte*)pThis + pThis->dbcc_size;
                } } }
            public unsafe long CbVariablePart { get { return this.PbMax - this.PbVariablePart; } }
            };

        // TODO: Define accessor method for data
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_HANDLE 
            {
            public int       dbch_size;
            public int       dbch_devicetype;
            public int       dbch_reserved;
            public IntPtr    dbch_handle;     // file handle used in call to RegisterDeviceNotification
            public IntPtr    dbch_hdevnotify; // returned from RegisterDeviceNotification
            //
            // The following 3 fields are only valid if wParam is DBT_CUSTOMEVENT.
            //
            public System.Guid  dbch_eventguid;
            public int          dbch_nameoffset; // offset (bytes) of variable-length string buffer (-1 if none)
        //  BYTE                dbch_data[1];    // variable-sized buffer, potentially containing binary and/or text data
            };


        //
        // Define 32-bit and 64-bit versions of the DEV_BROADCAST_HANDLE structure
        // for WOW64.  These must be kept in sync with the above structure.
        //

        // TODO: Define accessor method for data
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_HANDLE32 
            {
            public int          dbch_size;
            public int          dbch_devicetype;
            public int          dbch_reserved;
            public uint         dbch_handle;
            public uint         dbch_hdevnotify;
            public System.Guid  dbch_eventguid;
            public int          dbch_nameoffset;
        //  BYTE                dbch_data[1];
            };

        // TODO: Define accessor method for data
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct DEV_BROADCAST_HANDLE64 
            {
            public int          dbch_size;
            public int          dbch_devicetype;
            public int          dbch_reserved;
            public ulong        dbch_handle;
            public ulong        dbch_hdevnotify;
            public System.Guid  dbch_eventguid;
            public int          dbch_nameoffset;
        //  BYTE                dbch_data[1];
            };


        public const int DBTF_RESOURCE = 0x00000001;    // network resource
        public const int DBTF_XPORT = 0x00000002;       // new transport coming or going
        public const int DBTF_SLOWNET = 0x00000004;     // new incoming transport is slow
                                                        // (dbcn_resource undefined for now)

        public const int DBT_VPOWERDAPI = 0x8100;          // VPOWERD API for Win95

        /*
         *  User-defined message types all use wParam = 0xFFFF with the
         *  lParam a pointer to the structure below.
         *
         *  dbud_dbh - DEV_BROADCAST_HEADER must be filled in as usual.
         *
         *  dbud_szName contains a case-sensitive ASCIIZ name which names the
         *  message.  The message name consists of the vendor name, a backslash,
         *  then arbitrary user-defined ASCIIZ text.  For example:
         *
         *      "WidgetWare\QueryScannerShutdown"
         *      "WidgetWare\Video Q39S\AdapterReady"
         *
         *  After the ASCIIZ name, arbitrary information may be provided.
         *  Make sure that dbud_dbh.dbch_size is big enough to encompass
         *  all the data.  And remember that nothing in the structure may
         *  contain pointers.
         */

        public const int DBT_USERDEFINED = 0xFFFF;

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
        public struct DEV_BROADCAST_USERDEFINED 
            {
            public DEV_BROADCAST_HDR dbud_dbh;
        //  char        dbud_szName[1];     /* ASCIIZ name */
        /*  BYTE        dbud_rgbUserDefined[];*/ /* User-defined contents */

            public unsafe string dbud_szName { get
                { 
                return Marshal.PtrToStringAnsi(new IntPtr(this.PbVariablePart));
                }}

            public unsafe byte* PbUserDefined { get { fixed(DEV_BROADCAST_USERDEFINED* pThis = &this) 
                {
                return this.PbVariablePart + (this.dbud_szName.Length+1) * sizeof(byte);     // +1 for terminating null 
                } } }

            public unsafe int CbUserDefined { get { fixed(DEV_BROADCAST_USERDEFINED* pThis = &this) 
                {  
                return (int)(pThis->dbud_dbh.dbch_size - (this.PbUserDefined - (byte*)pThis));
                } } }

            public unsafe byte[] dbud_rgbUserDefined { get 
                {
                int cb = this.CbUserDefined;
                byte[] result = new byte[cb];
                Marshal.Copy(new IntPtr(this.PbUserDefined), result, 0, cb);
                return result;
                } }

            public unsafe byte* PbVariablePart { get { fixed(DEV_BROADCAST_USERDEFINED* pThis = &this) 
                { 
                return (byte*)(&pThis->dbud_dbh) + sizeof(DEV_BROADCAST_HDR); 
                } } }
            public unsafe byte* PbMax { get { fixed(DEV_BROADCAST_USERDEFINED* pThis = &this) 
                {
                return (byte*)pThis + pThis->dbud_dbh.dbch_size;
                } } }

            };


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

        //
        // USB 1.1: 9.6.5 String, Table 9-12. UNICODE String Descriptor
        // USB 2.0: 9.6.7 String, Table 9-16. UNICODE String Descriptor
        // USB 3.0: 9.6.8 String, Table 9-22. UNICODE String Descriptor
        //
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct USB_STRING_DESCRIPTOR
            {
            public byte bLength;
            public byte bDescriptorType;
            // public char bString[];

            public static int CbOverhead { get { return 2; } }
            }

        //------------------------------------------------------------------------------
        // Constants
        //------------------------------------------------------------------------------

        //
        // USB 1.1: 9.4 Standard Device Requests, Table 9-5. Descriptor Types
        //
        public const byte USB_DEVICE_DESCRIPTOR_TYPE                          = 0x01;
        public const byte USB_CONFIGURATION_DESCRIPTOR_TYPE                   = 0x02;
        public const byte USB_STRING_DESCRIPTOR_TYPE                          = 0x03;
        public const byte USB_INTERFACE_DESCRIPTOR_TYPE                       = 0x04;
        public const byte USB_ENDPOINT_DESCRIPTOR_TYPE                        = 0x05;

        public static Guid   IID_IUnknown         = new Guid("00000000-0000-0000-C000-000000000046");
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int BLUETOOTH_MAX_NAME_SIZE             = (248);
        public const int BLUETOOTH_MAX_PASSKEY_SIZE          = (16);
        public const int BLUETOOTH_MAX_PASSKEY_BUFFER_SIZE   = (BLUETOOTH_MAX_PASSKEY_SIZE + 1);
        public const int BLUETOOTH_MAX_SERVICE_NAME_SIZE     = (256);
        public const int BLUETOOTH_DEVICE_NAME_SIZE          = (256);

        //
        // Flags controlling what is included in the device information set built
        // by SetupDiGetClassDevs
        //
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

        public const uint DEVICE_SPEED                = ((uint)(1));
        public const byte USB_ENDPOINT_DIRECTION_MASK = ((byte)(0X80));

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
        SafeFileHandle SafeCreateFile(string lpFileName, 
            uint    dwDesiredAccess, 
            int     dwShareMode, 
            IntPtr  lpSecurityAttributes, 
            int     dwCreationDisposition, 
            int     dwFlagsAndAttributes, 
            IntPtr  hTemplateFile);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="CreateFileW")] public static extern 
        IntPtr CreateFile(string lpFileName, 
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
        IntPtr RegisterDeviceNotification(IntPtr hRecipient, DEV_BROADCAST_DEVICEINTERFACE_MANAGED filter, int Flags);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool SetCommState(IntPtr hFile, ref DCB dcb);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS timeouts);


        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        IntPtr SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, IntPtr hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInfo(IntPtr hDeviceInfoSet, int MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiDestroyDeviceInfoList(IntPtr hDeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, IntPtr DeviceInfoData, IntPtr InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)] public static extern 
        bool SetupDiEnumDeviceInterfaces(IntPtr hDeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(IntPtr pClassGuid,         IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern 
        IntPtr SetupDiGetClassDevsW(ref System.Guid ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern 
        bool SetupDiGetDeviceInterfaceDetail(
            IntPtr                                      hDeviceInfoSet, 
            ref SP_DEVICE_INTERFACE_DATA                DeviceInterfaceData, 
            ref SP_DEVICE_INTERFACE_DETAIL_DATA_MANAGED DeviceInterfaceDetailData, 
            int                                         DeviceInterfaceDetailDataSize, 
            out int                                     cbRequired, 
            IntPtr                                      DeviceInfoData
            );

        [DllImport("setupapi.dll", SetLastError =true, CharSet = CharSet.Unicode)] public static extern
        bool SetupDiGetDeviceInstanceIdW(
            IntPtr                                      hDeviceInfoSet,
            ref SP_DEVINFO_DATA                         DeviceInfoData,
            IntPtr                                      DeviceInstanceId,
            int                                         DeviceInstanceIdSize,
            out int                                     RequiredSize
            );

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

        [DllImport("winusb.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern
        bool WinUsb_Initialize(IntPtr handle, out IntPtr pWinUsbHandle);

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
        public static void ThrowWin32Error()
            {
            ThrowWin32Error(Marshal.GetLastWin32Error(), null);
            }
        public static void ThrowWin32Error(string message)
            {
            ThrowWin32Error(Marshal.GetLastWin32Error(), message);
            }
        public static void ThrowWin32Error(int err, string message)
            {
            switch (err)
                {
            case 0:
                return;
            default:
                if (message == null)
                    throw new System.ComponentModel.Win32Exception(err);
                else
                    throw new System.ComponentModel.Win32Exception(err, message);
                }
            }

        public static int Win32ErrorFromException(Exception e)
            {
            if (e is System.ComponentModel.Win32Exception)
                {
                return (e as System.ComponentModel.Win32Exception).NativeErrorCode;
                }
            return ERROR_SERVICE_SPECIFIC_ERROR;
            }


        public static int GetLastError()
            {
            return Marshal.GetLastWin32Error();
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

        //----------------------------------------------------------------------------------------------------------
        // Misc
        //----------------------------------------------------------------------------------------------------------

        public const int TRUE = 1;
        public const int FALSE = 0;

        //----------------------------------------------------------------------------------------------------------
        // Service related stuff
        //----------------------------------------------------------------------------------------------------------

        public const int ACCEPT_NETBINDCHANGE = 0x10;
        public const int ACCEPT_PARAMCHANGE = 8;
        public const int ACCEPT_PAUSE_CONTINUE = 2;
        public const int ACCEPT_POWEREVENT = 0x40;
        public const int ACCEPT_SESSIONCHANGE = 0x80;
        public const int ACCEPT_SHUTDOWN = 4;
        public const int ACCEPT_STOP = 1;
        public const int ACCESS_TYPE_ALL = 0xf01ff;
        public const int ACCESS_TYPE_CHANGE_CONFIG = 2;
        public const int ACCESS_TYPE_ENUMERATE_DEPENDENTS = 8;
        public const int ACCESS_TYPE_INTERROGATE = 0x80;
        public const int ACCESS_TYPE_PAUSE_CONTINUE = 0x40;
        public const int ACCESS_TYPE_QUERY_CONFIG = 1;
        public const int ACCESS_TYPE_QUERY_STATUS = 4;
        public const int ACCESS_TYPE_START = 0x10;
        public const int ACCESS_TYPE_STOP = 0x20;
        public const int ACCESS_TYPE_USER_DEFINED_CONTROL = 0x100;
        public const int BROADCAST_QUERY_DENY = 0x424d5144;
        public const int CONTROL_CONTINUE = 3;
        public const int CONTROL_DEVICEEVENT = 11;
        public const int CONTROL_INTERROGATE = 4;
        public const int CONTROL_NETBINDADD = 7;
        public const int CONTROL_NETBINDDISABLE = 10;
        public const int CONTROL_NETBINDENABLE = 9;
        public const int CONTROL_NETBINDREMOVE = 8;
        public const int CONTROL_PARAMCHANGE = 6;
        public const int CONTROL_PAUSE = 2;
        public const int CONTROL_POWEREVENT = 13;
        public const int CONTROL_SESSIONCHANGE = 14;
        public const int CONTROL_SHUTDOWN = 5;
        public const int CONTROL_STOP = 1;
        public static readonly string DATABASE_ACTIVE = "ServicesActive";
        public static readonly string DATABASE_FAILED = "ServicesFailed";
        public const int ERROR_CONTROL_CRITICAL = 3;
        public const int ERROR_CONTROL_IGNORE = 0;
        public const int ERROR_CONTROL_NORMAL = 1;
        public const int ERROR_CONTROL_SEVERE = 2;
        public const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        public const int ERROR_MORE_DATA = 0xea;
        public const int MAX_COMPUTERNAME_LENGTH = 0x1f;
        public const int MB_ABORTRETRYIGNORE = 2;
        public const int MB_APPLMODAL = 0;
        public const int MB_DEFAULT_DESKTOP_ONLY = 0x20000;
        public const int MB_DEFBUTTON1 = 0;
        public const int MB_DEFBUTTON2 = 0x100;
        public const int MB_DEFBUTTON3 = 0x200;
        public const int MB_DEFBUTTON4 = 0x300;
        public const int MB_DEFMASK = 0xf00;
        public const int MB_HELP = 0x4000;
        public const int MB_ICONASTERISK = 0x40;
        public const int MB_ICONERROR = 0x10;
        public const int MB_ICONEXCLAMATION = 0x30;
        public const int MB_ICONHAND = 0x10;
        public const int MB_ICONINFORMATION = 0x40;
        public const int MB_ICONMASK = 240;
        public const int MB_ICONQUESTION = 0x20;
        public const int MB_ICONWARNING = 0x30;
        public const int MB_MISCMASK = 0xc000;
        public const int MB_MODEMASK = 0x3000;
        public const int MB_NOFOCUS = 0x8000;
        public const int MB_OK = 0;
        public const int MB_OKCANCEL = 1;
        public const int MB_RETRYCANCEL = 5;
        public const int MB_RIGHT = 0x80000;
        public const int MB_RTLREADING = 0x100000;
        public const int MB_SERVICE_NOTIFICATION = 0x200000;
        public const int MB_SERVICE_NOTIFICATION_NT3X = 0x40000;
        public const int MB_SETFOREGROUND = 0x10000;
        public const int MB_SYSTEMMODAL = 0x1000;
        public const int MB_TASKMODAL = 0x2000;
        public const int MB_TOPMOST = 0x40000;
        public const int MB_TYPEMASK = 15;
        public const int MB_USERICON = 0x80;
        public const int MB_YESNO = 4;
        public const int MB_YESNOCANCEL = 3;
        public const int NO_ERROR = 0;
        public const int PBT_APMBATTERYLOW = 9;
        public const int PBT_APMOEMEVENT = 11;
        public const int PBT_APMPOWERSTATUSCHANGE = 10;
        public const int PBT_APMQUERYSUSPEND = 0;
        public const int PBT_APMQUERYSUSPENDFAILED = 2;
        public const int PBT_APMRESUMEAUTOMATIC = 0x12;
        public const int PBT_APMRESUMECRITICAL = 6;
        public const int PBT_APMRESUMESUSPEND = 7;
        public const int PBT_APMSUSPEND = 4;
        public const int POLICY_ALL_ACCESS = 0xf07ff;
        public const int POLICY_AUDIT_LOG_ADMIN = 0x200;
        public const int POLICY_CREATE_ACCOUNT = 0x10;
        public const int POLICY_CREATE_PRIVILEGE = 0x40;
        public const int POLICY_CREATE_SECRET = 0x20;
        public const int POLICY_GET_PRIVATE_INFORMATION = 4;
        public const int POLICY_LOOKUP_NAMES = 0x800;
        public const int POLICY_SERVER_ADMIN = 0x400;
        public const int POLICY_SET_AUDIT_REQUIREMENTS = 0x100;
        public const int POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x80;
        public const int POLICY_TRUST_ADMIN = 8;
        public const int POLICY_VIEW_AUDIT_INFORMATION = 2;
        public const int POLICY_VIEW_LOCAL_INFORMATION = 1;
        public const int SC_ENUM_PROCESS_INFO = 0;
        public const int SC_MANAGER_ALL = 0xf003f;
        public const int SERVICE_TYPE_ADAPTER = 4;
        public const int SERVICE_TYPE_ALL = 0x13f;
        public const int SERVICE_TYPE_DRIVER = 11;
        public const int SERVICE_TYPE_FILE_SYSTEM_DRIVER = 2;
        public const int SERVICE_TYPE_INTERACTIVE_PROCESS = 0x100;
        public const int SERVICE_TYPE_KERNEL_DRIVER = 1;
        public const int SERVICE_TYPE_RECOGNIZER_DRIVER = 8;
        public const int SERVICE_TYPE_WIN32 = 0x30;
        public const int SERVICE_TYPE_WIN32_OWN_PROCESS = 0x10;
        public const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 0x20;
        public const int STANDARD_RIGHTS_DELETE = 0x10000;
        public const int STANDARD_RIGHTS_REQUIRED = 0xf0000;
        public const int START_TYPE_AUTO = 2;
        public const int START_TYPE_BOOT = 0;
        public const int START_TYPE_DEMAND = 3;
        public const int START_TYPE_DISABLED = 4;
        public const int START_TYPE_SYSTEM = 1;
        public const int STATE_CONTINUE_PENDING = 5;
        public const int STATE_PAUSE_PENDING = 6;
        public const int STATE_PAUSED = 7;
        public const int STATE_RUNNING = 4;
        public const int STATE_START_PENDING = 2;
        public const int STATE_STOP_PENDING = 3;
        public const int STATE_STOPPED = 1;
        public const int STATUS_ACTIVE = 1;
        public const int STATUS_ALL = 3;
        public const int STATUS_INACTIVE = 2;
        public const int STATUS_OBJECT_NAME_NOT_FOUND = -1073741772;
        public const int WM_POWERBROADCAST = 0x218;
        public const int WTS_CONSOLE_CONNECT = 1;
        public const int WTS_CONSOLE_DISCONNECT = 2;
        public const int WTS_REMOTE_CONNECT = 3;
        public const int WTS_REMOTE_DISCONNECT = 4;
        public const int WTS_SESSION_LOCK = 7;
        public const int WTS_SESSION_LOGOFF = 6;
        public const int WTS_SESSION_LOGON = 5;
        public const int WTS_SESSION_REMOTE_CONTROL = 9;
        public const int WTS_SESSION_UNLOCK = 8;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ChangeServiceConfig2(IntPtr serviceHandle, uint infoLevel, ref SERVICE_DELAYED_AUTOSTART_INFO serviceDesc);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ChangeServiceConfig2(IntPtr serviceHandle, uint infoLevel, ref SERVICE_DESCRIPTION serviceDesc);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateService(IntPtr databaseHandle, string serviceName, string displayName, int access, int serviceType, int startType, int errorControl, string binaryPath, string loadOrderGroup, IntPtr pTagId, string dependencies, string servicesStartName, string password);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteService(IntPtr serviceHandle);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetComputerName(StringBuilder lpBuffer, ref int nSize);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LookupAccountName(string systemName, string accountName, byte[] sid, int[] sidLen, char[] refDomainName, int[] domNameLen, [In, Out] int[] sidNameUse);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaAddAccountRights(IntPtr policyHandle, byte[] accountSid, LSA_UNICODE_STRING userRights, int countOfRights);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaEnumerateAccountRights(IntPtr policyHandle, byte[] accountSid, out IntPtr pLsaUnicodeStringUserRights, out int RightsCount);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaOpenPolicy(LSA_UNICODE_STRING systemName, IntPtr pointerObjectAttributes, int desiredAccess, out IntPtr pointerPolicyHandle);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaRemoveAccountRights(IntPtr policyHandle, byte[] accountSid, bool allRights, LSA_UNICODE_STRING userRights, int countOfRights);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr databaseHandle, string serviceName, int access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandler(string serviceName, Delegate callback);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(string serviceName, Delegate callback, IntPtr userData);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool SetServiceStatus(IntPtr serviceStatusHandle, SERVICE_STATUS* status);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool StartServiceCtrlDispatcher(IntPtr entry);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class ENUM_SERVICE_STATUS
            {
            public string serviceName;
            public string displayName;
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class ENUM_SERVICE_STATUS_PROCESS
            {
            public string serviceName;
            public string displayName;
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            public int processID;
            public int serviceFlags;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_OBJECT_ATTRIBUTES
            {
            public int length;
            public IntPtr rootDirectory = IntPtr.Zero;
            public IntPtr pointerLsaString = IntPtr.Zero;
            public int attributes;
            public IntPtr pointerSecurityDescriptor = IntPtr.Zero;
            public IntPtr pointerSecurityQualityOfService = IntPtr.Zero;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_UNICODE_STRING
            {
            public short length;
            public short maximumLength;
            public string buffer;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_UNICODE_STRING_withPointer
            {
            public short length;
            public short maximumLength;
            public IntPtr pwstr = IntPtr.Zero;
            }

        [StructLayout(LayoutKind.Sequential)]
        public class QUERY_SERVICE_CONFIG
            {
            public int dwServiceType;
            public int dwStartType;
            public int dwErrorControl;
            public unsafe char* lpBinaryPathName;
            public unsafe char* lpLoadOrderGroup;
            public int dwTagId;
            public unsafe char* lpDependencies;
            public unsafe char* lpServiceStartName;
            public unsafe char* lpDisplayName;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SC_ACTION
            {
            public int type;
            public uint delay;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DELAYED_AUTOSTART_INFO
            {
            public bool fDelayedAutostart;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DESCRIPTION
            {
            public IntPtr description;
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_FAILURE_ACTIONS
            {
            public uint dwResetPeriod;
            public IntPtr rebootMsg;
            public IntPtr command;
            public uint numActions;
            public unsafe SC_ACTION* actions;
            }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
            {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            }

        [StructLayout(LayoutKind.Sequential)]
        public class SERVICE_TABLE_ENTRY
            {
            public IntPtr name;
            public Delegate callback;
            }

        public delegate void ServiceControlCallback(int control);

        public delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);

        public delegate void ServiceMainCallback(int argCount, IntPtr argPointer);

        [ComVisible(false)]
        public enum StructFormat
            {
            Ansi = 1,
            Auto = 3,
            Unicode = 2
            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class WTSSESSION_NOTIFICATION
            {
            public int size;
            public int sessionId;
            }

        //----------------------------------------------------------------------------------------------------------
        // WinSvc.h (incomplete)
        //----------------------------------------------------------------------------------------------------------

        //
        // Value to indicate no change to an optional parameter
        //
        public const int SERVICE_NO_CHANGE = -1;

        //
        // Service State -- for Enum Requests (Bit Mask)
        //
        public const int SERVICE_ACTIVE = 0x00000001;
        public const int SERVICE_INACTIVE = 0x00000002;
        public const int SERVICE_STATE_ALL = (SERVICE_ACTIVE   |  SERVICE_INACTIVE);

        //
        // Controls
        //
        public const int SERVICE_CONTROL_STOP = 0x00000001;
        public const int SERVICE_CONTROL_PAUSE = 0x00000002;
        public const int SERVICE_CONTROL_CONTINUE = 0x00000003;
        public const int SERVICE_CONTROL_INTERROGATE = 0x00000004;
        public const int SERVICE_CONTROL_SHUTDOWN = 0x00000005;
        public const int SERVICE_CONTROL_PARAMCHANGE = 0x00000006;
        public const int SERVICE_CONTROL_NETBINDADD = 0x00000007;
        public const int SERVICE_CONTROL_NETBINDREMOVE = 0x00000008;
        public const int SERVICE_CONTROL_NETBINDENABLE = 0x00000009;
        public const int SERVICE_CONTROL_NETBINDDISABLE = 0x0000000A;
        public const int SERVICE_CONTROL_DEVICEEVENT = 0x0000000B;
        public const int SERVICE_CONTROL_HARDWAREPROFILECHANGE = 0x0000000C;
        public const int SERVICE_CONTROL_POWEREVENT = 0x0000000D;
        public const int SERVICE_CONTROL_SESSIONCHANGE = 0x0000000E;
        public const int SERVICE_CONTROL_PRESHUTDOWN = 0x0000000F;
        public const int SERVICE_CONTROL_TIMECHANGE = 0x00000010;
        public const int SERVICE_CONTROL_TRIGGEREVENT = 0x00000020;
        public const int SERVICE_CONTROL_USERMODEREBOOT     = 0x40;
        public const int SERVICE_CONTROL_USERCODEFIRST      = 128;
        public const int SERVICE_CONTROL_USERCODELAST       = 255;

        //
        // Service State -- for CurrentState
        //
        public const int SERVICE_STOPPED = 0x00000001;
        public const int SERVICE_START_PENDING = 0x00000002;
        public const int SERVICE_STOP_PENDING = 0x00000003;
        public const int SERVICE_RUNNING = 0x00000004;
        public const int SERVICE_CONTINUE_PENDING = 0x00000005;
        public const int SERVICE_PAUSE_PENDING = 0x00000006;
        public const int SERVICE_PAUSED = 0x00000007;

        //
        // Controls Accepted  (Bit Mask)
        //
        public const int SERVICE_ACCEPT_STOP = 0x00000001;
        public const int SERVICE_ACCEPT_PAUSE_CONTINUE = 0x00000002;
        public const int SERVICE_ACCEPT_SHUTDOWN = 0x00000004;
        public const int SERVICE_ACCEPT_PARAMCHANGE = 0x00000008;
        public const int SERVICE_ACCEPT_NETBINDCHANGE = 0x00000010;
        public const int SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x00000020;
        public const int SERVICE_ACCEPT_POWEREVENT = 0x00000040;
        public const int SERVICE_ACCEPT_SESSIONCHANGE = 0x00000080;
        public const int SERVICE_ACCEPT_PRESHUTDOWN = 0x00000100;
        public const int SERVICE_ACCEPT_TIMECHANGE = 0x00000200;
        public const int SERVICE_ACCEPT_TRIGGEREVENT = 0x00000400;

        //
        // Service Control Manager object specific access types
        //
        public const int SC_MANAGER_CONNECT = 0x0001;
        public const int SC_MANAGER_CREATE_SERVICE = 0x0002;
        public const int SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
        public const int SC_MANAGER_LOCK = 0x0008;
        public const int SC_MANAGER_QUERY_LOCK_STATUS = 0x0010;
        public const int SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020;

        public const int SC_MANAGER_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED      | 
                                                    SC_MANAGER_CONNECT            | 
                                                    SC_MANAGER_CREATE_SERVICE     | 
                                                    SC_MANAGER_ENUMERATE_SERVICE  | 
                                                    SC_MANAGER_LOCK               | 
                                                    SC_MANAGER_QUERY_LOCK_STATUS  | 
                                                    SC_MANAGER_MODIFY_BOOT_CONFIG);
        //
        // Service object specific access type
        //
        public const int SERVICE_QUERY_CONFIG = 0x0001;
        public const int SERVICE_CHANGE_CONFIG = 0x0002;
        public const int SERVICE_QUERY_STATUS = 0x0004;
        public const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
        public const int SERVICE_START = 0x0010;
        public const int SERVICE_STOP = 0x0020;
        public const int SERVICE_PAUSE_CONTINUE = 0x0040;
        public const int SERVICE_INTERROGATE = 0x0080;
        public const int SERVICE_USER_DEFINED_CONTROL = 0x0100;

        public const int SERVICE_ALL_ACCESS =  (STANDARD_RIGHTS_REQUIRED     | 
                                                SERVICE_QUERY_CONFIG         | 
                                                SERVICE_CHANGE_CONFIG        | 
                                                SERVICE_QUERY_STATUS         | 
                                                SERVICE_ENUMERATE_DEPENDENTS | 
                                                SERVICE_START                | 
                                                SERVICE_STOP                 | 
                                                SERVICE_PAUSE_CONTINUE       | 
                                                SERVICE_INTERROGATE          | 
                                                SERVICE_USER_DEFINED_CONTROL);

        //
        // Service flags for QueryServiceStatusEx
        //
        public const int SERVICE_RUNS_IN_SYSTEM_PROCESS = 0x00000001;

        //
        // Info levels for ChangeServiceConfig2 and QueryServiceConfig2
        //
        public const int SERVICE_CONFIG_DESCRIPTION = 1;
        public const int SERVICE_CONFIG_FAILURE_ACTIONS = 2;
        public const int SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
        public const int SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 4;
        public const int SERVICE_CONFIG_SERVICE_SID_INFO = 5;
        public const int SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 6;
        public const int SERVICE_CONFIG_PRESHUTDOWN_INFO = 7;
        public const int SERVICE_CONFIG_TRIGGER_INFO = 8;
        public const int SERVICE_CONFIG_PREFERRED_NODE = 9;
        // reserved                                     10
        // reserved                                     11
        public const int SERVICE_CONFIG_LAUNCH_PROTECTED = 12;

        //
        // Info levels for NotifyServiceStatusChange
        //
        public const int SERVICE_NOTIFY_STATUS_CHANGE_1 = 1;
        public const int SERVICE_NOTIFY_STATUS_CHANGE_2 = 2;

        public const int SERVICE_NOTIFY_STATUS_CHANGE = SERVICE_NOTIFY_STATUS_CHANGE_2;

        //
        // Service notification masks
        //
        public const int SERVICE_NOTIFY_STOPPED = 0x00000001;
        public const int SERVICE_NOTIFY_START_PENDING = 0x00000002;
        public const int SERVICE_NOTIFY_STOP_PENDING = 0x00000004;
        public const int SERVICE_NOTIFY_RUNNING = 0x00000008;
        public const int SERVICE_NOTIFY_CONTINUE_PENDING = 0x00000010;
        public const int SERVICE_NOTIFY_PAUSE_PENDING = 0x00000020;
        public const int SERVICE_NOTIFY_PAUSED = 0x00000040;
        public const int SERVICE_NOTIFY_CREATED = 0x00000080;
        public const int SERVICE_NOTIFY_DELETED = 0x00000100;
        public const int SERVICE_NOTIFY_DELETE_PENDING = 0x00000200;

        //
        // The following defines are for service stop reason codes
        //

        //
        // Stop reason flags. Update SERVICE_STOP_REASON_FLAG_MAX when
        // new flags are added.
        //
        public const int SERVICE_STOP_REASON_FLAG_MIN = 0x00000000;
        public const int SERVICE_STOP_REASON_FLAG_UNPLANNED = 0x10000000;
        public const int SERVICE_STOP_REASON_FLAG_CUSTOM = 0x20000000;
        public const int SERVICE_STOP_REASON_FLAG_PLANNED = 0x40000000;
        public const uint SERVICE_STOP_REASON_FLAG_MAX = 0x80000000;

        //
        // Microsoft major reasons. Update SERVICE_STOP_REASON_MAJOR_MAX when
        // new codes are added.
        //
        public const int SERVICE_STOP_REASON_MAJOR_MIN = 0x00000000;
        public const int SERVICE_STOP_REASON_MAJOR_OTHER = 0x00010000;
        public const int SERVICE_STOP_REASON_MAJOR_HARDWARE = 0x00020000;
        public const int SERVICE_STOP_REASON_MAJOR_OPERATINGSYSTEM = 0x00030000;
        public const int SERVICE_STOP_REASON_MAJOR_SOFTWARE = 0x00040000;
        public const int SERVICE_STOP_REASON_MAJOR_APPLICATION = 0x00050000;
        public const int SERVICE_STOP_REASON_MAJOR_NONE = 0x00060000;
        public const int SERVICE_STOP_REASON_MAJOR_MAX = 0x00070000;
        public const int SERVICE_STOP_REASON_MAJOR_MIN_CUSTOM = 0x00400000;
        public const int SERVICE_STOP_REASON_MAJOR_MAX_CUSTOM = 0x00ff0000;

        //
        // Microsoft minor reasons. Update SERVICE_STOP_REASON_MINOR_MAX when
        // new codes are added.
        //
        public const int SERVICE_STOP_REASON_MINOR_MIN = 0x00000000;
        public const int SERVICE_STOP_REASON_MINOR_OTHER = 0x00000001;
        public const int SERVICE_STOP_REASON_MINOR_MAINTENANCE = 0x00000002;
        public const int SERVICE_STOP_REASON_MINOR_INSTALLATION = 0x00000003;
        public const int SERVICE_STOP_REASON_MINOR_UPGRADE = 0x00000004;
        public const int SERVICE_STOP_REASON_MINOR_RECONFIG = 0x00000005;
        public const int SERVICE_STOP_REASON_MINOR_HUNG = 0x00000006;
        public const int SERVICE_STOP_REASON_MINOR_UNSTABLE = 0x00000007;
        public const int SERVICE_STOP_REASON_MINOR_DISK = 0x00000008;
        public const int SERVICE_STOP_REASON_MINOR_NETWORKCARD = 0x00000009;
        public const int SERVICE_STOP_REASON_MINOR_ENVIRONMENT = 0x0000000a;
        public const int SERVICE_STOP_REASON_MINOR_HARDWARE_DRIVER = 0x0000000b;
        public const int SERVICE_STOP_REASON_MINOR_OTHERDRIVER = 0x0000000c;
        public const int SERVICE_STOP_REASON_MINOR_SERVICEPACK = 0x0000000d;
        public const int SERVICE_STOP_REASON_MINOR_SOFTWARE_UPDATE = 0x0000000e;
        public const int SERVICE_STOP_REASON_MINOR_SECURITYFIX = 0x0000000f;
        public const int SERVICE_STOP_REASON_MINOR_SECURITY = 0x00000010;
        public const int SERVICE_STOP_REASON_MINOR_NETWORK_CONNECTIVITY = 0x00000011;
        public const int SERVICE_STOP_REASON_MINOR_WMI = 0x00000012;
        public const int SERVICE_STOP_REASON_MINOR_SERVICEPACK_UNINSTALL = 0x00000013;
        public const int SERVICE_STOP_REASON_MINOR_SOFTWARE_UPDATE_UNINSTALL = 0x00000014;
        public const int SERVICE_STOP_REASON_MINOR_SECURITYFIX_UNINSTALL = 0x00000015;
        public const int SERVICE_STOP_REASON_MINOR_MMC = 0x00000016;
        public const int SERVICE_STOP_REASON_MINOR_NONE = 0x00000017;
        public const int SERVICE_STOP_REASON_MINOR_MAX = 0x00000018;
        public const int SERVICE_STOP_REASON_MINOR_MIN_CUSTOM = 0x00000100;
        public const int SERVICE_STOP_REASON_MINOR_MAX_CUSTOM = 0x0000FFFF;

        //
        // Info levels for ControlServiceEx
        //
        public const int SERVICE_CONTROL_STATUS_REASON_INFO = 1;

        //
        // Service SID types supported
        //
        public const int SERVICE_SID_TYPE_NONE = 0x00000000;
        public const int SERVICE_SID_TYPE_UNRESTRICTED = 0x00000001;
        public const int SERVICE_SID_TYPE_RESTRICTED = ( 0x00000002 | SERVICE_SID_TYPE_UNRESTRICTED );

        //
        // Service trigger types
        //
        public const int SERVICE_TRIGGER_TYPE_DEVICE_INTERFACE_ARRIVAL = 1;
        public const int SERVICE_TRIGGER_TYPE_IP_ADDRESS_AVAILABILITY = 2;
        public const int SERVICE_TRIGGER_TYPE_DOMAIN_JOIN = 3;
        public const int SERVICE_TRIGGER_TYPE_FIREWALL_PORT_EVENT = 4;
        public const int SERVICE_TRIGGER_TYPE_GROUP_POLICY = 5;
        public const int SERVICE_TRIGGER_TYPE_NETWORK_ENDPOINT = 6;
        public const int SERVICE_TRIGGER_TYPE_CUSTOM_SYSTEM_STATE_CHANGE = 7;
        public const int SERVICE_TRIGGER_TYPE_CUSTOM = 20;

        //
        // Service trigger data types
        //
        public const int SERVICE_TRIGGER_DATA_TYPE_BINARY = 1;
        public const int SERVICE_TRIGGER_DATA_TYPE_STRING = 2;
        public const int SERVICE_TRIGGER_DATA_TYPE_LEVEL = 3;
        public const int SERVICE_TRIGGER_DATA_TYPE_KEYWORD_ANY = 4;
        public const int SERVICE_TRIGGER_DATA_TYPE_KEYWORD_ALL = 5;

        //
        //  Service start reason
        //
        public const int SERVICE_START_REASON_DEMAND = 0x00000001;
        public const int SERVICE_START_REASON_AUTO = 0x00000002;
        public const int SERVICE_START_REASON_TRIGGER = 0x00000004;
        public const int SERVICE_START_REASON_RESTART_ON_FAILURE = 0x00000008;
        public const int SERVICE_START_REASON_DELAYEDAUTO = 0x00000010;

        //
        //  Service dynamic information levels
        //
        public const int SERVICE_DYNAMIC_INFORMATION_LEVEL_START_REASON = 1;

        //
        // Service LaunchProtected types supported
        //
        public const int SERVICE_LAUNCH_PROTECTED_NONE = 0;
        public const int SERVICE_LAUNCH_PROTECTED_WINDOWS = 1;
        public const int SERVICE_LAUNCH_PROTECTED_WINDOWS_LIGHT = 2;
        public const int SERVICE_LAUNCH_PROTECTED_ANTIMALWARE_LIGHT = 3;

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

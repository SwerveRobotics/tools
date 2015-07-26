//
// DirectInput.cs
//
// Glue code to just enough of DirectInput so that we can successfully
// read the joystick controllers. Not exactly pretty.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Org.SwerveRobotics.Tools.Library
    {
    //------------------------------------------------------------------------------------------------
    // Interfaces and DLL imports
    //------------------------------------------------------------------------------------------------

    [ComImport,Guid("BF798031-483A-4DA2-AA99-5D64ED369700"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDirectInput8W
        {
        void CreateDevice(
            [In] ref Guid rguid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IDirectInputDevice8W ppDevice,
            [In, MarshalAs(UnmanagedType.IUnknown)] object punkOuter
            );
        void EnumDevices(
            [In,MarshalAs(UnmanagedType.U4)] DI8DEVTYPE dwDevType,
            [In] IntPtr callback,
            [In] IntPtr pvRef,
            [In,MarshalAs(UnmanagedType.U4)] DIEDFL dwFlags
            );
        void _GetDeviceStatus();
        void _RunControlPanel();
        void Initialize(IntPtr hInstance, int dwVersion);
        void _FindDevice();
        void _EnumDevicesBySemantics();
        void _ConfigureDevices();
        }

    [ComImport,Guid("54D41081-DC15-4833-A41B-748F73A38179"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDirectInputDevice8W 
        {
        void _GetCapabilities();
        void EnumObjects(
            [In] IntPtr callback,
            [In] IntPtr pvRef,
            [In,MarshalAs(UnmanagedType.U4)] DIDFT dwFlags
            );
        void _GetProperty();
        void SetProperty(
            [In] IntPtr rguidProp,
            [In] ref DIPROPHEADER diph
            );
        [PreserveSig] int Acquire();
        [PreserveSig] int Unacquire();
        unsafe void GetDeviceState(
            [In, MarshalAs(UnmanagedType.U4)] int cbData,
            [In] void* pData
            );
        void _GetDeviceData();
        void SetDataFormat(
            [In] ref DIDATAFORMAT pdf
            );
        void _SetEventNotification();
        void _SetCooperativeLevel();
        void _GetObjectInfo();
        void _GetDeviceInfo();
        void _RunControlPanel();
        void _Initialize();
        void _CreateEffect();
        void _EnumEffects();
        void _GetEffectInfo();
        void _GetForceFeedbackState();
        void _SendForceFeedbackCommand();
        void _EnumCreatedEffectObjects();
        void _Escape();
        [PreserveSig] int Poll();
        void _SendDeviceData();
        void _EnumEffectsInFile();
        void _WriteEffectToFile();
        void _BuildActionMap();
        void _SetActionMap();
        void _GetImageInfo();
        }

    //------------------------------------------------------------------------------------------------
    // Delegates
    //------------------------------------------------------------------------------------------------

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int LPDIENUMDEVICESCALLBACK(ref DIDEVICEINSTANCEW pddi, IntPtr pvRef);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int DIEnumDeviceObjectsCallback(ref DIDEVICEOBJECTINSTANCE pddoi, IntPtr pvRef);

    //------------------------------------------------------------------------------------------------
    // Structs
    //------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DIDEVICEINSTANCEW
        {
        public int  dwSize;
        public Guid guidInstance;
        public Guid guidProduct;
        public int  dwDevType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=WIN32.MAX_PATH)] public string tszInstanceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=WIN32.MAX_PATH)] public string tszProductName;
        public Guid guidFFDriver;
        public short wUsagePage;
        public short wUsage;

        public DIDEVICEINSTANCEW Clone()
            {
            return (DIDEVICEINSTANCEW)(this.MemberwiseClone());
            }
        }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DIDEVICEOBJECTINSTANCE
        {
        public int   dwSize;
        public Guid  guidType;
        public int   dwOfs;
        public int   dwType;
        public int   dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=WIN32.MAX_PATH)] public string tszName;
        public int   dwFFMaxForce;
        public int   dwFFForceResolution;
        public short wCollectionNumber;
        public short wDesignatorIndex;
        public short wUsagePage;
        public short wUsage;
        public int   dwDimension;
        public short wExponent;
        public short wReportId;

        public DIDEVICEOBJECTINSTANCE Clone()
            {
            return (DIDEVICEOBJECTINSTANCE)(this.MemberwiseClone());
            }
        }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DIPROPRANGE
        {
        public DIPROPHEADER diph;
        public int lMin;
        public int lMax;
        }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DIPROPHEADER
        {
        public int dwSize;
        public int dwHeaderSize;
        public int dwObj;
        public int dwHow;
        }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public unsafe struct DIDATAFORMAT
        {
        public int    dwSize;
        public int    dwObjSize;
        public int    dwFlags;     // use DIDF_ABSAXIS
        public int    dwDataSize;
        public int    dwNumObjs;
        public DIOBJECTDATAFORMAT* rgodf;

        public void Init(int dwNumObjs)
            {
            this.dwSize     = Marshal.SizeOf(this);
            this.dwObjSize  = Marshal.SizeOf(typeof(DIOBJECTDATAFORMAT));
            this.dwFlags    = 0;
            this.dwDataSize = 0;
            this.rgodf      = null;
            this.dwNumObjs  = 0;
            Alloc(dwNumObjs);
            }

        public void Alloc(int dwNumObjs)
            {
            Free();
            //
            int cb         = this.dwObjSize * dwNumObjs;
            this.rgodf     = (DIOBJECTDATAFORMAT*)Marshal.AllocCoTaskMem(cb);
            this.dwNumObjs = dwNumObjs;
            for (int i = 0; i < this.dwNumObjs; i++)
                {
                this.rgodf[i].Init();
                }
            }

        public void Free()
            {
            if (this.rgodf != null)
                {
                for (int i = 0; i < this.dwNumObjs; i++)
                    {
                    this.rgodf[i].Free();
                    }
                Marshal.FreeCoTaskMem((IntPtr)this.rgodf);
                this.rgodf = null;
                this.dwNumObjs = 0;
                }
            }
        }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public unsafe struct DIOBJECTDATAFORMAT 
        {
        public Guid*  pguid;
        public int    dwOfs;
        public int    dwType;
        public int    dwFlags;

        public void Init()
            {
            this.pguid = null;
            Set(null, IntPtr.Zero, DIDFT.ALL, DIDOI.NONE);
            }

        public void Free()
            {
            FreeGuid();
            }

        public void Set(ref Guid guid, IntPtr dib, DIDFT didft, DIDOI flags)
            {
            fixed (Guid* pguid = &guid)
                {
                Set(pguid, dib, didft, flags);
                }
            }

        public void Set(Guid* pguid, IntPtr dib, DIDFT didft, DIDOI flags)
            {
            this.dwOfs    = (int)dib;
            this.dwType   = (int)didft;
            this.dwFlags  = (int)flags;
            if (null == pguid)
                {
                FreeGuid();
                }
            else
                {
                AllocGuid();
                *(this.pguid) = *pguid;
                }
            }

        private void AllocGuid()
            {
            if (null == this.pguid)
                {
                this.pguid = (Guid*)Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid)));
                }
            }
        private void FreeGuid()
            {
            if (null != this.pguid)
                {
                Marshal.FreeCoTaskMem((IntPtr)this.pguid);
                this.pguid = null;
                }
            }
        }

    //------------------------------------------------------------------------------------------------
    // Enums and other constants
    //------------------------------------------------------------------------------------------------

    public static class Guids
        {
        public static Guid XAxis  = new Guid("A36D02E0-C9F3-11CF-BFC7-444553540000");
        public static Guid YAxis  = new Guid("A36D02E1-C9F3-11CF-BFC7-444553540000");
        public static Guid ZAxis  = new Guid("A36D02E2-C9F3-11CF-BFC7-444553540000");
        public static Guid RZAxis = new Guid("A36D02E3-C9F3-11CF-BFC7-444553540000");
        public static Guid POV    = new Guid("A36D02F2-C9F3-11CF-BFC7-444553540000");
        };

    public enum DIDF
        {
        ABSAXIS = 1,
        RELAXIS = 2,
        }

    public enum DI8DEVTYPE
        {
        DEVICE           = 0x11,
        MOUSE            = 0x12,
        KEYBOARD         = 0x13,
        JOYSTICK         = 0x14,
        GAMEPAD          = 0x15,
        DRIVING          = 0x16,
        FLIGHT           = 0x17,
        FIRSTPERSON      = 0x18,
        DEVICECTRL       = 0x19,
        SCREENPOINTER    = 0x1A,
        REMOTE           = 0x1B,
        SUPPLEMENTAL     = 0x1C,
        CLASS_ALL        = 0,
        CLASS_DEVICE     = 1,
        CLASS_POINTER    = 2,
        CLASS_KEYBOARD   = 3,
        CLASS_GAMECTRL   = 4,
        };

    public enum DIEDFL
        {
        ALLDEVICES       = 0x00000000,
        ATTACHEDONLY     = 0x00000001,
        FORCEFEEDBACK    = 0x00000100,
        INCLUDEALIASES   = 0x00010000,
        INCLUDEPHANTOMS  = 0x00020000,
        INCLUDEHIDDEN    = 0x00040000,
        }

    public enum DIDFT : uint
        {
        ALL             = 0x00000000,
        RELAXIS         = 0x00000001,
        ABSAXIS         = 0x00000002,
        AXIS            = 0x00000003,
        PSHBUTTON       = 0x00000004,
        TGLBUTTON       = 0x00000008,
        BUTTON          = 0x0000000C,
        POV             = 0x00000010,
        COLLECTION      = 0x00000040,
        NODATA          = 0x00000080,
        ANYINSTANCE     = 0x00FFFF00,
        INSTANCEMASK    = ANYINSTANCE,
        FFACTUATOR      = 0x01000000,
        FFEFFECTTRIGGER = 0x02000000,
        OUTPUT          = 0x10000000,
        VENDORDEFINED   = 0x04000000,
        ALIAS           = 0x08000000,
        NOCOLLECTION    = 0x00FFFF00,
        MYSTERY         = 0x80000000,   // We seem to need this (and it's in c_dfDIJoystick2), but we don't know what it does
        }

    public enum DIDOI
        {
        NONE              = 0x00000000,
        FFACTUATOR        = 0x00000001,
        FFEFFECTTRIGGER   = 0x00000002,
        POLLED            = 0x00008000,
        ASPECTPOSITION    = 0x00000100,
        ASPECTVELOCITY    = 0x00000200,
        ASPECTACCEL       = 0x00000300,
        ASPECTFORCE       = 0x00000400,
        ASPECTMASK        = 0x00000F00,
        GUIDISUSAGE       = 0x00010000,
        }

    public enum DIPROP
        {
        BUFFERSIZE       = 1,
        AXISMODE         = 2,
        GRANULARITY      = 3,
        RANGE            = 4,
        DEADZONE         = 5,
        SATURATION       = 6,
        FFGAIN           = 7,
        FFLOAD           = 8,
        AUTOCENTER       = 9,
        CALIBRATIONMODE  = 10,
        CALIBRATION      = 11,
        GUIDANDPATH      = 12,
        INSTANCENAME     = 13,
        PRODUCTNAME      = 14,
        JOYSTICKID       = 15,
        GETPORTDISPLAYNAME  = 16,
        PHYSICALRANGE       = 18,
        LOGICALRANGE        = 19,
        KEYNAME             = 20,
        CPOINTS             = 21,
        APPDATA          = 22,
        SCANCODE         = 23,
        VIDPID           = 24,
        USERNAME         = 25,
        TYPENAME         = 26,
        }

    public enum DIPH
        {
        DEVICE      = 0,
        BYOFFSET    = 1,
        BYID        = 2,
        BYUSAGE     = 3,
        }

    //------------------------------------------------------------------------------------------------
    // Classes
    //------------------------------------------------------------------------------------------------

    public class DirectInputDevice
        {
        //--------------------------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------------------------

        IDirectInputDevice8W pInputDevice;

        //--------------------------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------------------------

        public DirectInputDevice(IDirectInputDevice8W pInputDevice)
            {
            this.pInputDevice = pInputDevice;
            }

        static int EnumObjectsCallback(ref DIDEVICEOBJECTINSTANCE pddoi, IntPtr pvRef)
            {
            GCHandle hResult = GCHandle.FromIntPtr(pvRef);
            List<DIDEVICEOBJECTINSTANCE> result = (List<DIDEVICEOBJECTINSTANCE>)(hResult.Target);
            result.Add(pddoi.Clone());
            return 1;
            }

        public List<DIDEVICEOBJECTINSTANCE> EnumObjects(DIDFT flags)
            {
            List<DIDEVICEOBJECTINSTANCE> result = new List<DIDEVICEOBJECTINSTANCE>();
            //
            DIEnumDeviceObjectsCallback callback = new DIEnumDeviceObjectsCallback(EnumObjectsCallback);
            GCHandle hCallback = GCHandle.Alloc(callback);
            GCHandle hResult   = GCHandle.Alloc(result);
            try {
                IntPtr pfn = Marshal.GetFunctionPointerForDelegate(callback);
                this.pInputDevice.EnumObjects(pfn, GCHandle.ToIntPtr(hResult), flags);
                }
            finally
                {
                hCallback.Free();
                hResult.Free();
                }
            //
            return result;
            }

        public void SetRange(DIDEVICEOBJECTINSTANCE o, int lowerRange, int upperRange)
            {
            DIPROPRANGE diproprange = new DIPROPRANGE();
            //
            diproprange.diph.dwSize       = Marshal.SizeOf(diproprange);
            diproprange.diph.dwHeaderSize = Marshal.SizeOf(diproprange.diph);
            diproprange.diph.dwObj        = o.dwType;
            diproprange.diph.dwHow        = (int)DIPH.BYID;
            diproprange.lMin              = lowerRange;
            diproprange.lMax              = upperRange;
            //
            this.pInputDevice.SetProperty((IntPtr)(DIPROP.RANGE), ref diproprange.diph);
            }

        public void SetDataFormat(ref DIDATAFORMAT pdf)
            {
            this.pInputDevice.SetDataFormat(pdf);
            }

        public void Acquire()   { Marshal.ThrowExceptionForHR(this.pInputDevice.Acquire());   }
        public void Unacquire() { Marshal.ThrowExceptionForHR(this.pInputDevice.Unacquire()); }
        public void Poll()      { Marshal.ThrowExceptionForHR(this.pInputDevice.Poll());      }
        public unsafe void GetDeviceState(int cbData, void* data) { this.pInputDevice.GetDeviceState(cbData, data); }
        }

    public class DirectInput
        {
        //--------------------------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------------------------

        IDirectInput8W pDirectInput8;

        //--------------------------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------------------------

        public DirectInput()
            {
            // Instantiate access to the main IDirectInput8 interface
            Guid   clsid = new Guid("25E609E4-B259-11CF-BFC7-444553540000");  // CLSID_DirectInput8
            Guid   iid   = WIN32.IIDOf(typeof(IDirectInput8W));
            object punk;
            int hr = WIN32.CoCreateInstance(ref clsid, null, WIN32.CLSCTX.INPROC_SERVER, ref iid, out punk);
            if (0==hr)
                {
                pDirectInput8 = (IDirectInput8W)punk;
                IntPtr hInstance = WIN32.GetModuleHandleW(null);
                pDirectInput8.Initialize(hInstance, 0x800);
                }
            else
                Marshal.ThrowExceptionForHR(hr);
            }

        static int EnumDevicesCallback(ref DIDEVICEINSTANCEW pddi, IntPtr pvRef)
            {
            GCHandle hResult = GCHandle.FromIntPtr(pvRef);
            List<DIDEVICEINSTANCEW> result = (List<DIDEVICEINSTANCEW>)(hResult.Target);
            result.Add(pddi.Clone());
            return 1;
            }

        public List<DIDEVICEINSTANCEW> EnumDevices(DI8DEVTYPE deviceType, DIEDFL flags)
            {
            List<DIDEVICEINSTANCEW> result = new List<DIDEVICEINSTANCEW>();
            //
            LPDIENUMDEVICESCALLBACK callback = new LPDIENUMDEVICESCALLBACK(DirectInput.EnumDevicesCallback);
            GCHandle hCallback = GCHandle.Alloc(callback);
            GCHandle hResult   = GCHandle.Alloc(result);
            try {
                IntPtr pfn = Marshal.GetFunctionPointerForDelegate(callback);
                this.pDirectInput8.EnumDevices(deviceType, pfn, GCHandle.ToIntPtr(hResult), flags);
                }
            finally
                {
                hCallback.Free();
                hResult.Free();
                }
            //
            return result;
            }

        public DirectInputDevice CreateDevice(Guid guid)
            {
            IDirectInputDevice8W pInputDevice;
            this.pDirectInput8.CreateDevice(ref guid, out pInputDevice, null);
            return new DirectInputDevice(pInputDevice);
            }
        }
    }

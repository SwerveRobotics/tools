using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using static Org.SwerveRobotics.BlueBotBug.Service.WIN32;

// https://madb.codeplex.com/SourceControl/latest#trunk/Managed.AndroidDebugBridge/Managed.Adb.Tests/BaseDeviceTests.cs

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    static class AdbWinApi
        {
        //-------------------------------------------------------------------------------------------------------------------
        // Constants
        //-------------------------------------------------------------------------------------------------------------------

        // http://binarydb.com/driver/Android-ADB-Interface-265790.html

        public const string         AdbWinApiDllName          = "AdbWinApi.dll";
        public readonly static Guid AndroidUsbDeviceClass     = new Guid("{3f966bd9-fa04-4ec5-991c-d326973b5128}");
        public readonly static Guid AndroidADBDeviceInterface = new Guid("{F72FE0D4-CBCB-407D-8814-9ED673D0DD6B}");

        //-------------------------------------------------------------------------------------------------------------------
        // Structs
        //-------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// A structure used by AdbEnumInterfaces to enumerate USB device interfaces of interest
        /// </summary>
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        public struct AdbInterfaceInfo_Managed
            {
            /// <summary>
            /// Interface's identifier (see SP_DEVICE_INTERFACE_DATA for details)
            /// </summary>
            public Guid         InterfaceId;
            /// <summary>
            /// Interface flags (see SP_DEVICE_INTERFACE_DATA for details)
            /// </summary>
            public uint         Flags;
            /// <summary>
            /// Device name for the interface (see SP_DEVICE_INTERFACE_DETAIL_DATA for details)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string       DevicePath;

            public static int CbMarshalledSize { get
                {
                int cbNonVariable = Marshal.OffsetOf(typeof(AdbInterfaceInfo_Managed), nameof(DevicePath)).ToInt32();
                return cbNonVariable + Marshal.SystemDefaultCharSize * MAX_PATH;
                }}
            }

        //-------------------------------------------------------------------------------------------------------------------
        // selected AdbWinAPI exports
        //-------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Enumerates all USB devices interfaces that match the indicated device interface GUID
        /// </summary>
        /// <param name="guidDeviceInterface">  the device interface GUID to search for</param>
        /// <param name="exclude_not_present">  if true, omit devices not currently present</param>
        /// <param name="exclude_removed">      if true, omit interfaces with the SPINT_REMOVED flat set</param>
        /// <param name="active_only">          if true, omit interfaces with the SPING_ACTIVE flat not set</param>
        /// <returns>                           null on failure</returns>
        [DllImport(AdbWinApiDllName, SetLastError = true)] public static extern 
        IntPtr AdbEnumInterfaces(Guid guidDeviceInterface, bool exclude_not_present, bool exclude_removed, bool active_only);

        /// <summary>
        /// Get the next USB device interface information in an enumeration
        /// </summary>
        /// <param name="hADB">             handle returned from AdbEnumInterfaces</param>
        /// <param name="pinfo">            (out) recepticle for the new information</param>
        /// <param name="pcb">              (in,out) on the way in provides size of the memory buffer
        ///                                 addressed by info parameter. On the way out (only if buffer was not
        ///                                 big enough) will provide memory size required for the next entry</param>
        /// <returns>true if success, false if failure</returns>
        [DllImport(AdbWinApiDllName, SetLastError = true)] public static extern 
        bool AdbNextInterface(IntPtr hADB, ref AdbInterfaceInfo_Managed pinfo, ref int pcb);

        /// <summary>
        /// Retrieves the serial number of a USB device interface
        /// </summary>
        /// <param name="hDeviceInterface">handle to the USB device interface, from OpenUSBDeviceInterface()</param>
        /// <param name="rgchBuffer">      the buffer into which the serial number is retrieved</param>
        /// <param name="pcchBuffer">      (in,out) the current/required size of the buffer in chars (the latter only if 
        ///                                GetLastError() is ERROR_INSUFFICIENT_BUFFER)</param>
        /// <param name="fAnsi">           whether we are to retrieve an ANSI or a Unicode serial number</param>
        /// <returns>true if success, false if failure</returns>
        [DllImport(AdbWinApiDllName, SetLastError = true)] public static extern 
        bool AdbGetSerialNumber(IntPtr hDeviceInterface, /*out*/IntPtr rgchBuffer, ref int pcchBuffer, bool fAnsi);

        /// <summary>
        /// Closes a handle previously opened with one of the AdbWinAPI calls
        /// </summary>
        /// <param name="handle"></param>
        [DllImport(AdbWinApiDllName, SetLastError = true)] public static extern 
        void AdbCloseHandle(IntPtr handle);


        /// <summary>
        /// Open a usb interface/device using its interface/device path
        /// </summary>
        /// <param name="deviceInterfacePath"></param>
        /// <returns>null on failure</returns>
        [DllImport(AdbWinApiDllName, SetLastError = true)] private static extern 
        IntPtr AdbCreateInterfaceByName(string deviceInterfacePath);

        /// <summary>
        /// Open a usb interface/device using its interface/device path
        /// </summary>
        /// <param name="deviceInterface"></param>
        /// <returns>the handle to the USB interface, or null on failure</returns>
        public static IntPtr OpenUSBDeviceInterface(AdbInterfaceInfo_Managed deviceInterface)
            {
            return OpenUSBDeviceInterface(deviceInterface.DevicePath);
            }
        public static IntPtr OpenUSBDeviceInterface(string deviceInterfacePath)
            {
            IntPtr result = AdbCreateInterfaceByName(deviceInterfacePath);
            if (result == IntPtr.Zero)
                ThrowWin32Error();
            return result;
            }
        }


    /// <summary>
    /// ADB manager manages our access to the Android ADB utility
    /// </summary>
    class ADBManager
        {
        }
    }

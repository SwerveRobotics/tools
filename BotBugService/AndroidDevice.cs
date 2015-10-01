using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Org.SwerveRobotics.Tools.ManagedADB;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    public class AndroidDevice
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        public string       USBSerialNumber         = null;
        public string       WifiDirectName          = null;
        public string       WlanIpAddress           = null;       // the IP address, if any, of the wlan0 address of this fellow
        public bool         WlanIsRunning           = false;

        public bool         IsConnected             = false;      // does ADB currently know about this guy?
        public bool         IsADBConnectedOnTcpip   = false;      // is the TPCIP endpoint of this device currently in ADB server's device list?
        public IPEndPoint   ConnectedEndpoint       = null;       // if IsADBConnectedOnTcpip, then this is where
        public bool         IsTCPIPOnLine           = false;      // if IsADBConnectedOnTcpip, then is it currently online?

        public string       UserIdentifier          => this.WifiDirectName ?? this.USBSerialNumber;

        private AndroidDeviceDatabase database;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public AndroidDevice(AndroidDeviceDatabase database)
            {
            this.database = database;
            }

        public AndroidDevice DisconnectedCopy()
            {
            AndroidDevice result = (AndroidDevice)this.MemberwiseClone();
            result.database = null;
            return result;
            }
        }


    public class AndroidDeviceDatabase
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        IDictionary<string,AndroidDevice>   mpUsbToDevice = new Dictionary<string, AndroidDevice>();

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------
        
        public AndroidDeviceDatabase()
            {
            }

        //-----------------------------------------------------------------------------------------
        // Operations
        //-----------------------------------------------------------------------------------------

        public AndroidDevice FromUSB(string usbSerialNumber)
            {
            if (string.IsNullOrEmpty(usbSerialNumber))
                throw new System.ArgumentException($"'{nameof(usbSerialNumber)}' cannot be null or empty");

            AndroidDevice result;
            if (!this.mpUsbToDevice.TryGetValue(usbSerialNumber, out result))
                {
                result = new AndroidDevice(this) { USBSerialNumber = usbSerialNumber };
                this.mpUsbToDevice[usbSerialNumber] = result;
                }
            return result;
            }

        public void UpdateFromDevicesConnectedToAdbServer(List<Device> devices)
            {
            foreach (AndroidDevice ad in this.mpUsbToDevice.Values)
                {
                ad.IsConnected        = false;
                ad.IsADBConnectedOnTcpip = false;
                ad.IsTCPIPOnLine      = false;
                }

            foreach (Device device in devices)
                {
                AndroidDevice ad = FromUSB(device.USBSerialNumber);
                ad.IsConnected        = true;
                ad.WifiDirectName     = device.WifiDirectName;
                ad.WlanIpAddress      = device.WlanIpAddress;
                ad.WlanIsRunning      = device.WlanIsRunning;

                if (device.SerialNumberIsTCPIP)
                    {
                    string[] pieces = device.SerialNumber.Split(':');
                    ad.ConnectedEndpoint     = new IPEndPoint(IPAddress.Parse(pieces[0]), Int32.Parse(pieces[1]));
                    ad.IsADBConnectedOnTcpip = true;
                    ad.IsTCPIPOnLine         = device.IsOnline;
                    }
                }
            }

        public IEnumerable<AndroidDevice> ConnectedDevices => this.mpUsbToDevice.Values.Where(ad => ad.IsConnected);
        }
    }
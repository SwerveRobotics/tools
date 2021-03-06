﻿using System;
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

        // The address at which robot controllers connect over Wifi Direct is fixed as the
        // controllers are always the group owner / access point in that case and Android
        // uses a fixed IP address for that role.
        public static readonly IPAddress WifiDirectIPAddress = IPAddress.Parse("192.168.49.1");

        public string       USBSerialNumber         = null;
        public string       WifiDirectName          = null;
        public string       WlanIpAddress           = null;       // the IP address, if any, of the wlan0 address of this fellow
        public bool         WlanIsRunning           = false;
        public string       IPAddressLastConnected  = null;       // where we last connected him at

        public List<string>     SerialNumbers           = new List<string>();       // current serial numbers we have for this guy and by which he is known in ADB
        public bool             IsConnected             = false;                    // does ADB currently know about this guy?
        public bool             IsAdbConnectedOnTcpip   => AdbEndpoints.Count > 0;  // is the TPCIP endpoint of this device currently in ADB server's device list?
        public List<IPEndPoint> AdbEndpoints            = new List<IPEndPoint>();   // if IsADBConnectedOnTcpip, then this is where
        public bool             IsTCPIPOnLine           = false;                    // if IsADBConnectedOnTcpip, then is it currently online?

        public string           UserIdentifier          => this.WifiDirectName ?? this.USBSerialNumber;

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

        public AndroidDevice FromUSBSerialNumber(string usbSerialNumber)
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
                ad.IsConnected     = false;
                ad.IsTCPIPOnLine   = false;
                ad.AdbEndpoints    = new List<IPEndPoint>();
                ad.SerialNumbers   = new List<string>();
                }

            foreach (Device device in devices)
                {
                if (!string.IsNullOrEmpty(device.USBSerialNumber))
                    {
                    AndroidDevice ad = FromUSBSerialNumber(device.USBSerialNumber);
                    ad.IsConnected        = true;
                    ad.WifiDirectName     = device.WifiDirectName;
                    ad.WlanIpAddress      = device.WlanIpAddress;
                    ad.WlanIsRunning      = device.WlanIsRunning;
                    ad.SerialNumbers.Add(device.SerialNumber);

                    if (device.SerialNumberIsTCPIP)
                        {
                        string[] pieces = device.SerialNumber.Split(':');
                        ad.AdbEndpoints.Add(new IPEndPoint(IPAddress.Parse(pieces[0]), int.Parse(pieces[1])));
                        ad.IsTCPIPOnLine = device.IsOnline;
                        }
                    }
                else
                    {
                    // We're hitting this occasionally, but we don't exactly know why. It seems like 
                    // the Win32 notifications might be racing with the ADB server notifications: this 
                    // only showed up on Bob's new super-duper-fast desktop. 
                    //
                    // We really ought to track down what's happening. But for now, we'll just disable
                    // the USB-notification path, which was always redundant, anyway.
                    }
                }
            }

        public bool IsWifiDirectIPAddressConnected => this.mpUsbToDevice.Values.SelectMany(device => device.AdbEndpoints).Any(ep => ep.Address.Equals(AndroidDevice.WifiDirectIPAddress));

        public IEnumerable<AndroidDevice> ConnectedDevices => this.mpUsbToDevice.Values.Where(ad => ad.IsConnected);
        }
    }
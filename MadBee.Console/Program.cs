using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.SwerveRobotics.Tools.ManagedADB;
using System.IO;

namespace Org.SwerveRobotics.Tools.MadBeeConsole
    {
    class Program
        {
        static void Main(string[] arguments)
            {
            AndroidDebugBridge bridge = new AndroidDebugBridge();
            try {
                bridge.DeviceConnected              += (sender, e) => System.Console.WriteLine($"Device connected: {e.Device.SerialNumber}\t{e.Device.State}");
                bridge.DeviceDisconnected           += (sender, e) => System.Console.WriteLine($"Device disconnected: {e.Device.SerialNumber}\t{e.Device.State}");
                bridge.ServerStartedOrReconnected   += (sender, b) => System.Console.WriteLine($"ADB server started or reconnected");
                bridge.ServerKilled                 += (sender, b) => System.Console.WriteLine($"ADB server killed");

                bridge.StartTracking();

                AdbHelper.Instance.Connect("192.168.0.22", 5555, AndroidDebugBridge.SocketAddress);

                System.Console.ReadLine();
                }
            finally
                {
                bridge.StopTracking();
                }
            }
        }
    }

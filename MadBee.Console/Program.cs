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
        public enum Actions
            {
            Devices,
            Monitor,
            Kill_Server,
            TCPIP,
            }

        static void Main(string[] arguments)
            {
            Arguments args = new Arguments(arguments);
            AndroidDebugBridge bridge = new AndroidDebugBridge();
            try {
                bridge.DeviceConnected      += (sender, e) => System.Console.WriteLine($"Device connected: {e.Device.SerialNumber}\t{e.Device.State}");
                bridge.DeviceDisconnected   += (sender, e) => System.Console.WriteLine($"Device disconnected: {e.Device.SerialNumber}\t{e.Device.State}");
                bridge.ServerStarted        += (sender, b) => System.Console.WriteLine($"ADB server started");
                bridge.ServerKilled         += (sender, b) => System.Console.WriteLine($"ADB server killed");

                bridge.StartTracking();

                System.Console.ReadLine();
                }
            finally
                {
                bridge.StopTracking();
                }
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Managed.Adb;
using System.IO;

namespace MadBee.Console
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
            Log.Level = LogLevel.Verbose;

            Arguments args = new Arguments(arguments);
            AndroidDebugBridge bridge = new AndroidDebugBridge();
            try {
                bridge.DeviceConnected      += (sender, e) => System.Console.WriteLine($"{e.Device.SerialNumber}\t{e.Device.State}");
                bridge.DeviceDisconnected   += (sender, e) => System.Console.WriteLine($"{e.Device.SerialNumber}\t{e.Device.State}");
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

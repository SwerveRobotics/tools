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

            var args = new Arguments(arguments);
            AndroidDebugBridge bridge = AndroidDebugBridge.OpenBridge(@"e:\ftc\tools\bin\debug\adb.exe", false);
            try {
                foreach (var item in Enum.GetNames(typeof(Actions)))
                    {
                    var actionName = item.Replace('_', '-').ToLower().Trim();
                    if (args.ContainsKey(actionName))
                        {
                        Actions action = (Actions)Enum.Parse(typeof(Actions), item, true);
                        switch (action)
                            {
                        case Actions.Devices:
                            foreach (var device in AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress))
                                {
                                System.Console.WriteLine("{0}\t{1}", device.SerialNumber, device.State);
                                }
                            break;
                        case Actions.Monitor:
                            bridge.DeviceChanged += (sender, e) =>
                                {
                                System.Console.WriteLine("Changed: {0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };
                            bridge.DeviceConnected += (sender, e) =>
                                {
                                System.Console.WriteLine("{0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };
                            bridge.DeviceDisconnected += (sender, e) =>
                                {
                                System.Console.WriteLine("{0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };
                            bridge.BridgeChanged += (sender, e) =>
                                {
                                if (e.Bridge == null)
                                    System.Console.WriteLine("null bridge");
                                else
                                    {
                                    System.Console.WriteLine("-----");
                                    foreach (var device in e.Bridge.Devices)
                                        {
                                        System.Console.WriteLine($"   device: {device.SerialNumber}");
                                        }
                                    }
                                };
                            System.Console.ReadLine();
                            break;
                        case Actions.Kill_Server:
                            try
                                {
                                AndroidDebugBridge.CloseBridge();
                                }
                            catch (IOException e)
                                {
                                System.Console.WriteLine(e.ToString());
                                // ignore
                                }
                            break;
                        case Actions.TCPIP:
                            {
                            bridge.DeviceChanged += delegate (object sender, DeviceEventArgs e)
                                {
                                System.Console.WriteLine("Changed: {0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };
                            bridge.DeviceConnected += delegate (object sender, DeviceEventArgs e)
                                {
                                System.Console.WriteLine("{0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };
                            bridge.DeviceDisconnected += delegate (object sender, DeviceEventArgs e)
                                {
                                System.Console.WriteLine("{0}\t{1}", e.Device.SerialNumber, e.Device.State);
                                };

                            List<Device> devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);

                            foreach (Device device in devices)
                                {
                                string ipPattern = "[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}\\.*[0-9]{1,3}:[0-9]{1,5}";
                                if (!device.SerialNumber.IsMatch(ipPattern))
                                    {
                                    // Find the device's IP address
                                    string ipAddress = device.GetProperty("dhcp.wlan0.ipaddress");

                                    // Restart the device listening on a port of interest
                                    int portNumber = 5555;
                                    AdbHelper.Instance.TcpIp(portNumber, AndroidDebugBridge.SocketAddress, device);

                                    // Connect to the TCPIP version of that device
                                    AdbHelper.Instance.Connect(ipAddress, portNumber, AndroidDebugBridge.SocketAddress);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                            }

                        return;
                        }
                    }

                System.Console.WriteLine("usage: (to be provided)");
                }
            finally
                {
                bridge.StopMonitoring();
                }
            }
        }
    }

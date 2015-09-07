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
            Start_Server,
            Kill_Server,
            Experiment,
            }

        static void Main(string[] arguments)
            {
            var args = new Arguments(arguments);
            AndroidDebugBridge bridge = AndroidDebugBridge.CreateBridge(@"e:\ftc\tools\bin\debug\adb.exe", true);
            foreach (var item in Enum.GetNames(typeof(Actions)))
                {
                var actionName = item.Replace('_', '-').ToLower().Trim();
                if (args.ContainsKey(actionName))
                    {
                    Actions action = (Actions)Enum.Parse(typeof(Actions), item, true);
                    switch (action)
                        {
                    case Actions.Devices:
                        GetDevices();
                        break;
                    case Actions.Monitor:
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
                        System.Console.ReadLine();
                        break;
                    case Actions.Start_Server:
                        StartServer();
                        break;
                    case Actions.Kill_Server:
                        break;
                    case Actions.Experiment:
                        {
                        List<Device> devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);
                        foreach (Device device in devices)
                            {
                            AdbHelper.Instance.TcpIp(5555, AndroidDebugBridge.SocketAddress, device);
                            // "service.adb.tcp.port"
                            // bridge.ExecuteRawSocketCommand(address, "host:devices-l")
                            // AdbHelper.Instance.SetDevice(, device);
                            }
                        }
                        break;
                    default:
                        break;
                        }
                    try
                        {
                        AndroidDebugBridge.DisconnectBridge();
                        bridge.Stop();
                        }
                    catch (IOException e)
                        {
                        System.Console.WriteLine(e.ToString());
                        // ignore
                        }

                    return;
                    }
                }

            PrintUsage();
            }

        private static void StartServer()
            {

            }


        private static void PrintUsage()
            {
            System.Console.WriteLine("Print Usage: ");
            }

        private static void GetDevices()
            {
            foreach (var device in AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress))
                {
                System.Console.WriteLine("{0}\t{1}", device.SerialNumber, device.State);
                }
            }


        }
    }

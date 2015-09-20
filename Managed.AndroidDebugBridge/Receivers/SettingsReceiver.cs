using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.ManagedADB.Receivers
    {
    class SettingsReceiver : MultiLineReceiver
        {
        public const string WIFI_P2P_DEVICE_NAME = "wifi_p2p_device_name";

        public enum NAMESPACE { system, secure, global };

        Device          device;
        NAMESPACE       ns;
        string          setting;


        public SettingsReceiver(Device device, NAMESPACE ns, string setting)
            {
            this.device  = device;
            this.ns      = ns;
            this.setting = setting;
            }

        public string Command { get 
            {
            return $"settings get {ns} {setting}";
            }}

        public void Execute()
            {
            this.device.ExecuteShellCommand(this.Command, this);
            }

        protected override void ProcessNewLines(string[] lines)
            {
            foreach (string line in lines)
                {
                switch (this.ns)
                    {
                case NAMESPACE.global:
                    this.device.Settings.Global.Add(this.setting, line);
                    break;
                case NAMESPACE.secure:
                    this.device.Settings.Secure.Add(this.setting, line);
                    break;
                case NAMESPACE.system:
                    this.device.Settings.System.Add(this.setting, line);
                    break;
                    }
                }
            }
        }
    }

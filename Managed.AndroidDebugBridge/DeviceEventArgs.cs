using System;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public class DeviceEventArgs : EventArgs
        {
        public DeviceEventArgs(Device device)
            {
            this.Device = device;
            }

        public Device Device { get; private set; }
        }
    }
using System;

namespace Managed.Adb
    {
    public class DeviceEventArgs : EventArgs
        {
        public DeviceEventArgs(IDevice device)
            {
            this.Device = device;
            }

        public IDevice Device { get; private set; }
        }
    }
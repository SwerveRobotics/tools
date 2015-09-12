using System;

namespace Managed.Adb
    {
    public class AndroidDebugBridgeEventArgs : EventArgs
        {
        public AndroidDebugBridge Bridge { get; private set; }

        public AndroidDebugBridgeEventArgs(AndroidDebugBridge bridge)
            {
            this.Bridge = bridge;
            }
        }
    }
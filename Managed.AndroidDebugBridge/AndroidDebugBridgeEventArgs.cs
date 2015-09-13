using System;

namespace Org.SwerveRobotics.Tools.ManagedADB
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
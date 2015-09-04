using System.Collections.Generic;
using NativeUsbLib;

namespace UsbEnumeration
{
    /// <summary>
    /// typ of the usb device
    /// </summary>
    public enum DeviceTyp
    {
        /// <summary>
        /// unknown device
        /// </summary>
        Unknown,
        /// <summary>
        /// controller
        /// </summary>
        Controller,
        /// <summary>
        /// root hub
        /// </summary>
        RootHub,
        /// <summary>
        /// hub
        /// </summary>
        Hub,
        /// <summary>
        /// device
        /// </summary>
        Device
    }

    /// <summary>
    /// Node to enumerate usb devices
    /// </summary>
    public class UsbTreeNode
    {
        /// <summary>
        /// Text used for TreeView, specifics on physical location of device(port #, etc)
        /// </summary>
        public string NodeText { get; set; }

		/// <summary>
		/// Device description
		/// </summary>
	    public string Description { get; set; }

        /// <summary>
        /// Parent node, null means root
        /// </summary>
        public UsbTreeNode Parent { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<UsbTreeNode> Children { get; set; }

        /// <summary>
        /// Gets the device.
        /// </summary>
        /// <value>The device.</value>
        public Device Device { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public DeviceTyp Type { get; set; }

		public UsbTreeNode()
        {
            Type = DeviceTyp.Unknown;
            Device = null;
            Children = new List<UsbTreeNode>();
        }
    }
}
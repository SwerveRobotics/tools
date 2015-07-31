using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Library
    {
    internal class USBMonitor
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        BlueBotBug bug    = null;
        ITracer    tracer = null;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        internal USBMonitor(BlueBotBug bug, ITracer tracer)
            {
            this.bug = bug;
            this.tracer = tracer;
            }

        internal void Start()
            {
            this.bug.DeviceArrived        += OnDeviceArrived;
            this.bug.DeviceRemoveComplete += OnDeviceRemoveComplete;
            }

        internal void Stop()
            {
            this.bug.DeviceArrived        -= OnDeviceArrived;
            this.bug.DeviceRemoveComplete -= OnDeviceRemoveComplete;
            }

        //-----------------------------------------------------------------------------------------
        // Events
        //-----------------------------------------------------------------------------------------

        unsafe void OnDeviceArrived(object sender, BlueBotBug.DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == WIN32.DBT_DEVTYP_DEVICEINTERFACE)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (WIN32.DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                Trace(pintf);
                }
            }

        unsafe void OnDeviceRemoveComplete(object sender, BlueBotBug.DeviceEventArgs args)
            {
            if (args.pHeader->dbch_devicetype == WIN32.DBT_DEVTYP_DEVICEINTERFACE)
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf = (WIN32.DEV_BROADCAST_DEVICEINTERFACE_W*)args.pHeader;
                Trace(pintf);
                }
            }

        unsafe void Trace(WIN32.DEV_BROADCAST_DEVICEINTERFACE_W* pintf)
            {
            this.tracer.Trace("name={0}", pintf->dbcc_name);
            this.tracer.Trace("guid={0}", pintf->dbcc_classguid);
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Library
    {
    public class AndroidDebuggerConfigerator 
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        ITracer tracer;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public AndroidDebuggerConfigerator(ITracer tracer)
            {
            this.tracer = tracer;
            }

        //-----------------------------------------------------------------------------------------
        // Events
        //-----------------------------------------------------------------------------------------

        public void OnAndroidDeviceArrived(object sender, USBDeviceInterface device)
            {
            tracer.Trace("Android arrived", device);
            }

        public void OnAndroidDeviceRemoved(object sender, USBDeviceInterface device)
            {
            tracer.Trace("Android removed", device);
            }
        }
    }

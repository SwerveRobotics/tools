using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Org.SwerveRobotics.Tools.Library;

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    public partial class BlueBotBugService : DecompiledServiceBase
        {
        //------------------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------------------
        
        private System.Diagnostics.EventLog eventLog;

        private const string eventLogSourceName = "BlueBotBug";
        private const string eventLogName       = "Application";

        private Tools.Library.BlueBotBug library = null;

        //------------------------------------------------------------------------------------------
        // Construction
        //------------------------------------------------------------------------------------------

        public BlueBotBugService()
            {
            InitializeComponent();

            this.eventLog = new EventLog();
            if (!EventLog.SourceExists(eventLogSourceName))
                {
                EventLog.CreateEventSource(eventLogSourceName, eventLogName);
                }
            eventLog.Source = eventLogSourceName;
            eventLog.Log    = eventLogName;
            }

        //------------------------------------------------------------------------------------------
        // Notifications
        //------------------------------------------------------------------------------------------

        protected override void OnStart(string[] args)
            {
            this.eventLog.WriteEntry("starting");
            this.library = new Tools.Library.BlueBotBug();
            this.library.Start();
            }

        protected override void OnStop()
            {
            this.eventLog.WriteEntry("stopping");
            if (this.library != null)
                { 
                library.Stop();
                library = null;
                }
            }

        protected unsafe override bool ShouldDeferCustomCommandEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
            {
            bool result = true;
            switch (command)
                {
            case WIN32.SERVICE_CONTROL_DEVICEEVENT:
                WIN32.DEV_BROADCAST_HDR* pHeader = (WIN32.DEV_BROADCAST_HDR*)eventData;
                result = this.library.ShouldDeferDeviceEvent(eventType, pHeader);
                break;
                }
            return result;
            }

        protected unsafe override int OnCustomCommandEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
            {
            int result = WIN32.NO_ERROR;
            switch (command)
                {
            case WIN32.SERVICE_CONTROL_DEVICEEVENT:
                WIN32.DEV_BROADCAST_HDR* pHeader = (WIN32.DEV_BROADCAST_HDR*)eventData;
                result = this.library.OnDeviceEvent(eventType, pHeader);
                break;
                }
            return result;
            }

        //------------------------------------------------------------------------------------------
        // Tracing
        //------------------------------------------------------------------------------------------

        void Trace(string format, params object[] args)
            {
            Util.Trace(format, args);
            }
        }
    }

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
    public partial class BlueBotBugService : ServiceBase
        {
        //------------------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------------------
        
        private System.Diagnostics.EventLog eventLog;

        private const string eventLogSourceName = "BlueBotBug";
        private const string eventLogName       = "BlueBotBug Events";

        private Tools.Library.BlueBotBug bluebotbug = null;

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
            this.bluebotbug = new Tools.Library.BlueBotBug();
            this.bluebotbug.Start();
            }

        protected override void OnStop()
            {
            this.eventLog.WriteEntry("stopping");
            if (this.bluebotbug != null)
                { 
                bluebotbug.Stop();
                bluebotbug = null;
                }
            }

        protected override void OnContinue()
            {
            this.eventLog.WriteEntry("continuing");
            }

        protected override void OnPause()
            {
            this.eventLog.WriteEntry("pausing");
            }

        protected override void OnShutdown()
            {
            this.eventLog.WriteEntry("shutting down");
            }
        }
    }

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BlueBotBugService
    {
    public partial class BlueBotBugService : ServiceBase
        {
        //------------------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------------------
        
        //------------------------------------------------------------------------------------------
        // Construction
        //------------------------------------------------------------------------------------------

        public BlueBotBugService()
            {
            InitializeComponent();
            }

        //------------------------------------------------------------------------------------------
        // Notifications
        //------------------------------------------------------------------------------------------

        protected override void OnStart(string[] args)
            {
            }

        protected override void OnStop()
            {
            }

        protected override void OnContinue()
            {
            }

        protected override void OnPause()
            {
            }

        protected override void OnShutdown()
            {
            }
        }
    }

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    // Helpful:
    // https://msdn.microsoft.com/en-us/library/vstudio/kz0ke5xt%28v=vs.100%29.aspx?f=255&MSPPError=-2147217396
    // misexec /i mysetup.msi /l*v mylog.txt
    
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
        {
        //--------------------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------------------
        
        const string key = "installing";

        void SetInstalling(InstallEventArgs e, bool value)
            {
            e.SavedState[key] = value;
            }

        bool IsInstalling(InstallEventArgs e)
            {
            return e.SavedState.Contains(key) && ((bool)e.SavedState[key]);
            }

        //--------------------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------------------
        
        public ProjectInstaller()
            {
            InitializeComponent();
            }

        //--------------------------------------------------------------------------------------
        // Operations
        //--------------------------------------------------------------------------------------

        public override void Install(IDictionary savedState)
            {
            TraceService("calling Install...");
            base.Install(savedState);
            TraceService("...Install called");
            }
        public override void Uninstall(IDictionary savedState)
            {
            TraceService("calling Uninstall...");
            base.Uninstall(savedState);
            TraceService("...Uninstall called");
            }
        public override void Commit(IDictionary savedState)
            {
            TraceService("calling Commit...");
            base.Commit(savedState);
            TraceService("...Commit called");
            }
        public override void Rollback(IDictionary savedState)
            {
            TraceService("calling Rollback...");
            base.Rollback(savedState);
            TraceService("...Rollback called");
            }

        //--------------------------------------------------------------------------------------
        // Service events
        //--------------------------------------------------------------------------------------

        private void OnServiceInstallerOnBeforeInstall(object sender, InstallEventArgs e)
            {
            TraceService("before install");
            SetInstalling(e, true);
            }
        private void OnServiceInstallerOnAfterInstall(object sender, InstallEventArgs e)
            {
            TraceService("after install");
            }
        private void OnServiceInstallerOnBeforeUninstall(object sender, InstallEventArgs e)
            {
            TraceService("before uninstall");
            SetInstalling(e, false);
            StopService();
            }
        private void OnServiceInstallerOnAfterUninstall(object sender, InstallEventArgs e)
            {
            TraceService("after uninstall");
            }

        private void OnServiceInstallerOnCommitting(object sender, InstallEventArgs e)
            {
            TraceService("committing");
            }
        private void OnServiceInstallerOnCommitted(object sender, InstallEventArgs e)
            {
            TraceService("committed");
            if (IsInstalling(e))
                StartService();
            }
        private void OnServiceInstallerOnBeforeRollback(object sender, InstallEventArgs e)
            {
            TraceService("before rollback");
            if (!IsInstalling(e))
                StopService();
            }
        private void OnServiceInstallerOnAfterRollback(object sender, InstallEventArgs e)
            {
            TraceService("after rollback");
            if (!IsInstalling(e))
                StartService();
            }

        //--------------------------------------------------------------------------------------
        // ServiceProcess events
        //--------------------------------------------------------------------------------------

        private void serviceProcessInstaller_BeforeInstall(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("before install");
            }

        private void serviceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("after install");
            }

        private void serviceProcessInstaller_BeforeUninstall(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("before uninstall");
            }

        private void serviceProcessInstaller_AfterUninstall(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("after uninstall");
            }

        private void serviceProcessInstaller_Committing(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("committing");
            }

        private void serviceProcessInstaller_Committed(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("committed");
            }

        private void serviceProcessInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("before rollback");
            }

        private void serviceProcessInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            TraceServiceProcess("after rollback");
            }

        //--------------------------------------------------------------------------------------
        // Utility
        //--------------------------------------------------------------------------------------
       
        void StartService()
            {
            Trace("starting service...");
            using (ServiceController sc = new ServiceController(this.serviceInstaller.ServiceName))
                {
                sc.Start();
                }
            Trace("...started");
            }

        void StopService()
            {
            Trace("stopping service...");
            try
                {
                using (ServiceController sc = new ServiceController(this.serviceInstaller.ServiceName))
                    {
                    sc.Stop();
                    }
                }
            catch (Exception)
                {
                // ignored
                }
            Trace("...stopped");
            }

        void Trace(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: installer: {message}");
            }
        void TraceService(string message)
            {
            Trace($"service:        {message}");
            }
        void TraceServiceProcess(string message)
            {
            Trace($"serviceProcess: {message}");
            }

        }
    }

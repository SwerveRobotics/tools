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
        // Events
        //--------------------------------------------------------------------------------------

        public override void Install(IDictionary savedState)
            {
            Trace("calling Install...");
            base.Install(savedState);
            Trace("...Install called");
            // Explicitly throw an exception so that roll back is called. 
            // throw new ArgumentException("Arg Exception");
            }
        public override void Uninstall(IDictionary savedState)
            {
            Trace("calling Uninstall...");
            base.Uninstall(savedState);
            Trace("...Uninstall called");
            }
        public override void Commit(IDictionary savedState)
            {
            Trace("calling Commit...");
            base.Commit(savedState);
            Trace("...Commit called");
            }
        public override void Rollback(IDictionary savedState)
            {
            Trace("calling Rollback...");
            base.Rollback(savedState);
            Trace("...Rollback called");
            }

        private void OnServiceInstallerOnBeforeInstall(object sender, InstallEventArgs e)
            {
            Trace("before install");
            SetInstalling(e, true);
            }
        private void OnServiceInstallerOnAfterInstall(object sender, InstallEventArgs e)
            {
            Trace("after install");
            }
        private void OnServiceInstallerOnBeforeUninstall(object sender, InstallEventArgs e)
            {
            Trace("before uninstall");
            SetInstalling(e, false);
            StopService();
            }
        private void OnServiceInstallerOnAfterUninstall(object sender, InstallEventArgs e)
            {
            Trace("after uninstall");
            }

        private void OnServiceInstallerOnCommitting(object sender, InstallEventArgs e)
            {
            Trace("committing");
            }
        private void OnServiceInstallerOnCommitted(object sender, InstallEventArgs e)
            {
            Trace("committed");
            if (IsInstalling(e))
                StartService();
            }
        private void OnServiceInstallerOnBeforeRollback(object sender, InstallEventArgs e)
            {
            Trace("before rollback");
            }
        private void OnServiceInstallerOnAfterRollback(object sender, InstallEventArgs e)
            {
            Trace("after rollback");
            if (!IsInstalling(e))
                StartService();
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
            try {
                using (ServiceController sc = new ServiceController(this.serviceInstaller.ServiceName))
                    {
                    sc.Stop();
                    }
                }
            catch (Exception)
                {
                // ignore
                }
            Trace("...stopped");
            }

        void Trace(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: installer: {message}");
            }
        }
    }

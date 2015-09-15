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
        
        const string keyInstalling = "installing";
        const string keyRunning    = "serviceWasRunning";

        void SetInstalling(InstallEventArgs e, bool value)
            {
            e.SavedState[keyInstalling] = value;
            }
        void SetServiceWasRunning(InstallEventArgs e, bool value)
            {
            e.SavedState[keyRunning] = value;
            }

        bool ServiceWasRunning(InstallEventArgs e) => e.SavedState.Contains(keyRunning) && ((bool)e.SavedState[keyRunning]);
        bool        Installing(InstallEventArgs e) => e.SavedState.Contains(keyInstalling) && ((bool)e.SavedState[keyInstalling]);
        bool      Uninstalling(InstallEventArgs e) => !Installing(e);

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
            TraceProject("Calling Install...");
            base.Install(savedState);
            TraceProject("...Install Called");
            }
        public override void Uninstall(IDictionary savedState)
            {
            TraceProject("Calling Uninstall...");
            base.Uninstall(savedState);
            TraceProject("...Uninstall Called");
            }
        public override void Commit(IDictionary savedState)
            {
            TraceProject("Calling Commit...");
            base.Commit(savedState);
            TraceProject("...Commit called");
            }
        public override void Rollback(IDictionary savedState)
            {
            TraceProject("Calling Rollback...");
            base.Rollback(savedState);
            TraceProject("...Rollback Called");
            }

        //--------------------------------------------------------------------------------------
        // Operations
        //--------------------------------------------------------------------------------------

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("before install");
                SetInstalling(e, true);
                });
            }
        private void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("after install");
                });
           }
        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("before uninstall");
                SetInstalling(e, false);
                });
            }
        private void ProjectInstaller_AfterUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("after uninstall");
                });
            }
        private void ProjectInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("before rollback");
                });
            }
        private void ProjectInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("after rollback");
                });
            }
        private void ProjectInstaller_Committing(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("committing");
                });
            }
        private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("committed");
                });
            }

        //--------------------------------------------------------------------------------------
        // Service events
        //--------------------------------------------------------------------------------------

        private void OnServiceInstallerOnBeforeInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("before install");
                SetInstalling(e, true);
                
                // Remember whether the service was running, then stop it.
                // All because having the service running during a setup
                // just isn't a good thing.
                SetServiceWasRunning(e, ServiceIsRunning());
                StopService();
                });
            }
        private void OnServiceInstallerOnAfterInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("after install");
                });
            }
        private void OnServiceInstallerOnBeforeUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("before uninstall");
                SetInstalling(e, false);

                // Remember whether the service was running, then stop it.
                // All because having the service running during a setup 
                // just isn't a good thing.
                SetServiceWasRunning(e, ServiceIsRunning());
                StopService();
                });
            }
        private void OnServiceInstallerOnAfterUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("after uninstall");
                });
            }

        private void OnServiceInstallerOnBeforeRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("before rollback");
                });
            }
        private void OnServiceInstallerOnAfterRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("after rollback");
                if (Installing(e))
                    {
                    // Rolling back an install: set the service state to what it
                    // was before we came in here.
                    if (ServiceWasRunning(e))
                        StartService();
                    else
                        StopService();
                    }
                else
                    {
                    // Does this ever happen?
                    TraceService("**** unexpected: rolling back an uninstall ****");
                    }
                });
            }

        private void OnServiceInstallerOnCommitting(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("committing");
                });
            }
        private void OnServiceInstallerOnCommitted(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceService("committed");
                // Finished successful install: start
                if (Installing(e))
                    StartService();
                });
            }

        //--------------------------------------------------------------------------------------
        // ServiceProcess events
        //--------------------------------------------------------------------------------------

        private void serviceProcessInstaller_BeforeInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("before install");
                });
            }

        private void serviceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("after install");
                });
            }

        private void serviceProcessInstaller_BeforeUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("before uninstall");
                });
            }

        private void serviceProcessInstaller_AfterUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("after uninstall");
                });
            }

        private void serviceProcessInstaller_Committing(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("committing");
                });
            }

        private void serviceProcessInstaller_Committed(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("committed");
                });
            }

        private void serviceProcessInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("before rollback");
                });
            }

        private void serviceProcessInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceServiceProcess("after rollback");
                });
            }

        //--------------------------------------------------------------------------------------
        // Utility
        //--------------------------------------------------------------------------------------
        
        void ReportExceptions(Action action)
            {
            try
                {
                action.Invoke();
                }
            catch (Exception e)
                {
                Trace($"exception ignored: {e}");
                }
            }

        bool ServiceIsRunning()
            {
            bool result = false;
            Trace("querying service status...");
            using (ServiceController sc = new ServiceController(this.serviceInstaller.ServiceName))
                {
                switch (sc.Status)
                    {
                case ServiceControllerStatus.Stopped:
                case ServiceControllerStatus.StopPending:
                    result = false;
                    break;
                default:
                    result = true;
                    break;
                    }
                }
            Trace($"...service is {(result ? "running" : "stopped")} ");
            return result;
            }
       
        void StartService()
            {
            Trace("starting service...");
            using (ServiceController sc = new ServiceController(this.serviceInstaller.ServiceName))
                {
                sc.Start();     // may throw
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
            catch (Exception e)
                {
                Trace($"StopService: exception ignored: {e}");
                }
            Trace("...stopped");
            }

        void Trace(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: installer: {message}");
            }
        void TraceProject(string message)
            {
            Trace($"project: {message}");
            }
        void TraceService(string message)
            {
            Trace($"service: {message}");
            }
        void TraceServiceProcess(string message)
            {
            Trace($"serviceProcess: {message}");
            }

        }
    }

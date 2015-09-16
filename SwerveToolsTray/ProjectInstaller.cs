using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Org.SwerveRobotics.Tools.Util;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    // http://devcity.net/PrintArticle.aspx?ArticleID=339
    // http://blogs.msdn.com/b/rflaming/archive/2006/09/23/768248.aspx

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

        bool Installing(InstallEventArgs e)
            {
            return e.SavedState.Contains(key) && ((bool)e.SavedState[key]);
            }
        bool Uninstalling(InstallEventArgs e)
            {
            return !Installing(e);
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
            TraceProject("...Commit Called");
            }
        public override void Rollback(IDictionary savedState)
            {
            TraceProject("Calling Rollback...");
            base.Rollback(savedState);
            TraceProject("...Rollback Called");
            }

        //--------------------------------------------------------------------------------------
        // Installation events
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
                StopApplication();
                });
            }

        private void ProjectInstaller_AfterUninstall(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("after uninstall");
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
                // Finished successful install: start
                //if (Installing(e))
                //    StartApplication();       // we now start using the MSI directly (we leave stop for good measure, though)
                });
            }

        private void ProjectInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("before rollback");
                // About to rollback an install: stop
                if (Installing(e))
                    StopApplication();
                });
            }

        private void ProjectInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            ReportExceptions(() => 
                {
                TraceProject("after rollback");
                // Finished rolling back an uninstall: start
                //if (Uninstalling(e))
                //    StartApplication();       // we now start using the MSI directly (we leave stop for good measure, though)
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
                TraceProject($"exception ignored: {e}");
                }
            }
       
        string GetExeName()
            {
            return Assembly.GetExecutingAssembly().Location;
            }
       
        void StartApplication()
            {
            TraceProject("starting application...");
            TraceProject($"path={GetExeName()}");
            System.Diagnostics.Process.Start(GetExeName());
            TraceProject("...started");
            }

        void StopApplication()
            {
            TraceProject("stopping application...");
            TraceProject($"path={GetExeName()}");
            try {
                (new ShutdownMonitor(Program.TrayUniquifier)).RequestShutdown();
                }
            catch (Exception e)
                {
                TraceProject($"StopApplication: exception ignored: {e}");
                }
            TraceProject("...stopped");
            }

        void TraceProject(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: installer: tray: {message}");
            }

        }
    }

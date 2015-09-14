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
        // Installation events
        //--------------------------------------------------------------------------------------

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
            {
            Trace("before install");
            SetInstalling(e, true);
            }

        private void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            Trace("after install");
            }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
            {
            Trace("before uninstall");
            SetInstalling(e, false);
            StopApplication();
            }

        private void ProjectInstaller_AfterUninstall(object sender, InstallEventArgs e)
            {
            Trace("after uninstall");
            }

        private void ProjectInstaller_Committing(object sender, InstallEventArgs e)
            {
            Trace("committing");
            }

        private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
            {
            Trace("committed");
            // Finished successful install: start
            if (Installing(e))
                StartApplication();
            }

        private void ProjectInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            Trace("before rollback");
            // About to rollback an install: stop
            if (Installing(e))
                StopApplication();
            }

        private void ProjectInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            Trace("after rollback");
            // Finished rolling back an uninstall: start
            if (Uninstalling(e))
                StartApplication();
            }

        //--------------------------------------------------------------------------------------
        // Utility
        //--------------------------------------------------------------------------------------
        
        string GetExeName()
            {
            return Assembly.GetExecutingAssembly().Location;
            }
       
        void StartApplication()
            {
            Trace("starting application...");
            Trace($"path={GetExeName()}");
            System.Diagnostics.Process.Start(GetExeName());
            Trace("...started");
            }

        void StopApplication()
            {
            Trace("stopping application...");
            Trace($"path={GetExeName()}");
            (new ShutdownMonitor(Program.TrayUniquifier)).RequestShutdown();
            Trace("...stopped");
            }

        void Trace(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: SwerveToolsTray: installer: {message}");
            }

        }
    }

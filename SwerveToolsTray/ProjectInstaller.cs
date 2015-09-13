using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
        {
        public ProjectInstaller()
            {
            InitializeComponent();
            }

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
            {
            Trace("before install");
            }

        private void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            Trace("after install");
            }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
            {
            Trace("before uninstall");
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
            }

        private void ProjectInstaller_BeforeRollback(object sender, InstallEventArgs e)
            {
            Trace("before rollback");
            }

        private void ProjectInstaller_AfterRollback(object sender, InstallEventArgs e)
            {
            Trace("after rollback");
            }

        //--------------------------------------------------------------------------------------
        // Utility
        //--------------------------------------------------------------------------------------
       
        void StartApplication()
            {
            Trace("starting application...");
            Trace("...started");
            }

        void StopApplication()
            {
            Trace("stopping application...");
            Trace("...started");
            }

        void Trace(string message)
            {
            System.Diagnostics.Trace.WriteLine($"BotBug: SwerveToolsTray: installer: {message}");
            }

        }
    }

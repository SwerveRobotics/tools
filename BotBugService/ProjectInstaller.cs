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
        public ProjectInstaller()
            {
            InitializeComponent();
            }

        private void serviceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
            {
            System.Diagnostics.Trace.WriteLine("BlueBotBug installer: starting service...");
            ServiceController sc = new ServiceController(serviceInstaller.ServiceName);
            sc.Start();
            }

        }
    }

using System.Configuration.Install;
using System.ServiceProcess;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    partial class ProjectInstaller
        {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
            {
            if (disposing && (components != null))
                {
                components.Dispose();
                }
            base.Dispose(disposing);
            }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
            {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            this.serviceProcessInstaller.Committed += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_Committed);
            this.serviceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_AfterInstall);
            this.serviceProcessInstaller.AfterRollback += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_AfterRollback);
            this.serviceProcessInstaller.AfterUninstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_AfterUninstall);
            this.serviceProcessInstaller.Committing += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_Committing);
            this.serviceProcessInstaller.BeforeInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_BeforeInstall);
            this.serviceProcessInstaller.BeforeRollback += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_BeforeRollback);
            this.serviceProcessInstaller.BeforeUninstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_BeforeUninstall);
            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "Auto configures FTC robot controllers for wireless debugging";
            this.serviceInstaller.DisplayName = "BotBug Service";
            this.serviceInstaller.ServiceName = "BotBug";
            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller.Committed += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnCommitted);
            this.serviceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnAfterInstall);
            this.serviceInstaller.AfterRollback += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnAfterRollback);
            this.serviceInstaller.AfterUninstall += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnAfterUninstall);
            this.serviceInstaller.Committing += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnCommitting);
            this.serviceInstaller.BeforeInstall += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnBeforeInstall);
            this.serviceInstaller.BeforeRollback += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnBeforeRollback);
            this.serviceInstaller.BeforeUninstall += new System.Configuration.Install.InstallEventHandler(this.OnServiceInstallerOnBeforeUninstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstaller});

            }

        #endregion
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceInstaller;
        }
    }
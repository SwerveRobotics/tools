using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.SwerveRobotics.Tools.SwerveToolsTray.Properties;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    class TrayApplicationContext : ApplicationContext
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------
        
        NotifyIcon  trayIcon;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        public TrayApplicationContext()
            {
            Application.ApplicationExit += (object sender, EventArgs e) =>
                {
                if (this.trayIcon!=null) 
                    this.trayIcon.Visible = false;
                };
            InitializeComponent();
            this.trayIcon.Visible = true;
            }

        private void InitializeComponent()
            {
            this.trayIcon = new NotifyIcon();
            this.trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            this.trayIcon.Text           = Resources.TrayIconText;
            this.trayIcon.Icon           = SystemIcons.Exclamation; // TODO
            }
        }
    }

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
    class TrayApplicationContext : ApplicationContext, IDisposable
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------
        
        NotifyIcon                  trayIcon;
        Util.BotBugSharedMemory     sharedMemory;
        bool                        disposed;

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
            this.disposed = false;
            }
        ~TrayApplicationContext()
            {
            this.Dispose(false);
            }

        private void InitializeComponent()
            {
            this.trayIcon = new NotifyIcon();
            this.trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            this.trayIcon.Text           = Resources.TrayIconText;
            this.trayIcon.Icon           = SystemIcons.Exclamation; // TODO
            }

        void IDisposable.Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected override void Dispose(bool fromUserCode)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (fromUserCode)
                    {
                    // Called from user's code. Can / should cleanup managed objects
                    this.sharedMemory?.Dispose();
                    this.sharedMemory = null;
                    }

                // Called from finalizers (and user code). Avoid referencing other objects.
                }
            base.Dispose(fromUserCode);
            }

        }
    }

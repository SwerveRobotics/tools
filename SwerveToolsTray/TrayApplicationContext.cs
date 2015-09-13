using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Org.SwerveRobotics.Tools.SwerveToolsTray.Properties;
using Org.SwerveRobotics.Tools.Util;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    class TrayApplicationContext : ApplicationContext, IDisposable
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------
        
        NotifyIcon                  trayIcon;
        SharedMemoryStringQueue     sharedMemory;
        bool                        stopRequested;
        bool                        disposed;
        ManualResetEvent            threadStartedEvent;
        Thread                      notificationThread;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        public TrayApplicationContext()
            {
            this.disposed = false;
            InitializeComponent();
            this.sharedMemory = new SharedMemoryStringQueue("BotBug");

            Application.ApplicationExit += (object sender, EventArgs e) => this.trayIcon.Visible = false;
            this.trayIcon.Visible = true;

            StartNotificationThread();
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

            MenuItem menuItem = new MenuItem(Resources.MenuItemExit, (sender, e) =>
                {
                this.trayIcon.Visible = false;  // be doubly sure
                ExitThread();
                });
            this.trayIcon.ContextMenu = new ContextMenu(new MenuItem[] { menuItem });
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
                    StopNotificationThread();
                    this.sharedMemory?.Dispose();
                    this.sharedMemory = null;
                    }

                // Called from finalizers (and user code). Avoid referencing other objects.
                }
            base.Dispose(fromUserCode);
            }

        //----------------------------------------------------------------------------
        // Notification
        //----------------------------------------------------------------------------
        
        void StartNotificationThread()
            {
            this.stopRequested = false;
            this.threadStartedEvent = new ManualResetEvent(false);
            this.notificationThread = new Thread(this.NotificationThreadLoop);
            this.notificationThread.Name = "Swerve Tray Notification Thread";
            this.notificationThread.Start();
            this.threadStartedEvent.WaitOne();
            }

        void StopNotificationThread()
            {
            if (this.notificationThread != null)
                {
                this.stopRequested = true;
                this.notificationThread.Interrupt();
                this.notificationThread.Join();
                this.notificationThread = null;
                this.threadStartedEvent.Reset();
                }
            }

        void NotificationThreadLoop()
            {
            // Interlock with StartNotificationThread
            this.threadStartedEvent.Set();

            while (!this.stopRequested)
                {
                try {
                    // Get messages from writer. This will block until there's
                    // (probably) messages for us to read
                    List<string> messages = this.sharedMemory.Read();
                    if (messages.Count > 0)
                        {
                        StringBuilder balloonText = new StringBuilder();
                        foreach (string message in messages)
                            {
                            if (balloonText.Length > 0)
                                balloonText.Append("\n");
                            balloonText.Append(message);
                            }

                        this.trayIcon.BalloonTipTitle = Resources.TrayIconBalloonTipTitle;
                        this.trayIcon.BalloonTipText = balloonText.ToString();
                        this.trayIcon.ShowBalloonTip(10000);
                        }
                    }
                catch (ThreadInterruptedException)
                    {
                    return;
                    }
                }
            }

        }
    }

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Org.SwerveRobotics.Tools.SwerveToolsTray.Properties;
using Org.SwerveRobotics.Tools.Util;
using static Org.SwerveRobotics.Tools.Util.Util;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    // http://stackoverflow.com/questions/14723843/notifyicon-remains-in-tray-even-after-application-closing-but-disappears-on-mous
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
        ShutdownMonitor             shutdownMonitor;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        public TrayApplicationContext()
            {
            this.disposed = false;
            InitializeComponent();
            this.sharedMemory    = new SharedMemoryStringQueue(false, "BotBug");    // uniquifier name must match that in BotBugService
            this.shutdownMonitor = new ShutdownMonitor(Program.TrayUniquifier);
            this.shutdownMonitor.ShutdownEvent += (sender, e) => ShutdownApp();
            this.shutdownMonitor.StartMonitoring();

            Application.ApplicationExit += (object sender, EventArgs e) => this.trayIcon.Visible = false;
            this.trayIcon.Visible = true;

            StartBotBugNotificationThread();
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
            this.trayIcon.Icon           = Util.Properties.Resources.SwerveIcon;

            MenuItem menuItem = new MenuItem(Resources.MenuItemExit, (sender, e) => ShutdownApp());
            this.trayIcon.ContextMenu = new ContextMenu(new MenuItem[] { menuItem });
            }

        void ShutdownApp()
            {
            this.trayIcon.Visible = false;  // be doubly sure
            ExitThread();
            }

        void IDisposable.Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected override void Dispose(bool notFinalizer)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (notFinalizer)
                    {
                    // Called from user's code. Can / should cleanup managed objects
                    StopBotBugNotificationThread();
                    this.shutdownMonitor?.StopMonitoring();
                    }
                // Called from finalizers (and user code). Avoid referencing other objects.
                this.sharedMemory?.Dispose();
                this.sharedMemory = null;
                }
            base.Dispose(notFinalizer);
            }

        //----------------------------------------------------------------------------
        // Notification
        //----------------------------------------------------------------------------
        
        void StartBotBugNotificationThread()
            {
            this.stopRequested = false;
            this.threadStartedEvent = new ManualResetEvent(false);
            this.notificationThread = new Thread(this.NotificationThreadLoop);
            this.notificationThread.Name = "Swerve Tray Notification Thread";
            this.notificationThread.Start();
            this.threadStartedEvent.WaitOne();
            }

        void StopBotBugNotificationThread()
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
            Trace(Program.LoggingTag, "===== NotificationThreadLoop start ... ");
            try {
                // Interlock with StartNotificationThread
                this.threadStartedEvent.Set();

                // Spin, waiting for kernel to make the section for us
                for (bool thrown = true; !this.stopRequested && thrown; )
                    {
                    try {
                        thrown = false;
                        this.sharedMemory.Initialize();
                        }
                    catch (FileNotFoundException)
                        {
                        thrown = true;
                        Thread.Sleep(2000);
                        }
                    }

                Trace(Program.LoggingTag, "===== NotificationThreadLoop listening");

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
            finally
                {
                Trace(Program.LoggingTag, "===== ... NotificationThreadLoop stop");
                }

            }

        }
    }

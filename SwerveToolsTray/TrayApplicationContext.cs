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
        
        NotifyIcon                      trayIcon;
        string                          statusText;
        SharedTaggedMemoryStringQueue   sharedMemory;
        bool                            disposed;
        HandshakeThreadStarter          threadStarter;
        ShutdownMonitor                 shutdownMonitor;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        public TrayApplicationContext()
            {
            this.disposed = false;
            InitializeComponent();
            this.statusText      = null;
            this.sharedMemory    = new SharedTaggedMemoryStringQueue(false, "BotBug");    // uniquifier name must match that in BotBugService
            this.shutdownMonitor = new ShutdownMonitor(Program.TrayUniquifier);
            this.shutdownMonitor.ShutdownEvent += (sender, e) => ShutdownApp();

            this.threadStarter = new HandshakeThreadStarter("Swerve Tray Notification Thread", this.NotificationThreadLoop);

            Application.ApplicationExit += (object sender, EventArgs e) => this.trayIcon.Visible = false;
            this.trayIcon.Visible = true;

            this.shutdownMonitor.StartMonitoring();
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
            this.trayIcon.Icon = Util.Properties.Resources.SwerveLogo;
            UpdateIconText();

            MenuItem menuItem = new MenuItem(Resources.MenuItemExit, (sender, e) => ShutdownApp());
            this.trayIcon.ContextMenu = new ContextMenu(new MenuItem[] { menuItem });
            }

        void UpdateIconText()
            {
            if (string.IsNullOrEmpty(this.statusText))
                this.trayIcon.Text = $"{Resources.TrayIconText}";
            else
                this.trayIcon.Text = $"{Resources.TrayIconText}: {this.statusText}";
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
                this.trayIcon?.Dispose();      this.trayIcon = null;
                this.sharedMemory?.Dispose();  this.sharedMemory = null;
                this.threadStarter?.Dispose(); this.threadStarter = null;
                }
            base.Dispose(notFinalizer);
            }

        //----------------------------------------------------------------------------
        // Notification
        //----------------------------------------------------------------------------
        
        void StartBotBugNotificationThread()
            {
            this.threadStarter.Start();
            }

        void StopBotBugNotificationThread()
            {
            this.threadStarter.Stop();
            }

        void NotificationThreadLoop(HandshakeThreadStarter starter)
            {
            Trace(Program.LoggingTag, "===== NotificationThreadLoop start ... ");
            try {
                // Interlock with StartNotificationThread
                starter.Handshake();

                // Spin, waiting for kernel to make the section for us
                for (bool thrown = true; !starter.StopRequested && thrown; )
                    {
                    try {
                        thrown = false;
                        this.sharedMemory.Initialize();
                        }
                    catch (FileNotFoundException)
                        {
                        Trace(Program.LoggingTag, "service hasn't created shared mem");
                        thrown = true;
                        Thread.Sleep(2000);
                        }
                    catch (Exception e)
                        {
                        Trace(Program.LoggingTag, $"exception thrown: {e}");
                        }
                    }

                Trace(Program.LoggingTag, "===== NotificationThreadLoop listening");

                while (!starter.StopRequested)
                    {
                    try {
                        // Get messages from writer. This will block until there's
                        // (probably) messages for us to read
                        Trace(Program.LoggingTag, "waiting for message...");
                        List<TaggedMessage> messages = this.sharedMemory.Read();
                        Trace(Program.LoggingTag, "...messages received");
                        if (messages.Count > 0)
                            {
                            // Separate the messages with newlines.
                            StringBuilder balloonText = new StringBuilder();
                            foreach (TaggedMessage taggedMessage in messages)
                                {
                                if (balloonText.Length > 0)
                                    balloonText.Append("\n");
                                if (taggedMessage.Tag == TaggedMessage.TagMessage)
                                    {
                                    balloonText.Append(taggedMessage.Message);
                                    }
                                else if (taggedMessage.Tag == TaggedMessage.TagStatus)
                                    {
                                    // Update the status text
                                    this.statusText = taggedMessage.Message;
                                    UpdateIconText();
                                    }
                                }

                            // Display them to the user
                            ShowBalloon(balloonText.ToString());
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

        void ShowBalloon(string text)
            {
            Trace(Program.LoggingTag, $"showing balloon: '{text}'");
            this.trayIcon.BalloonTipTitle = Resources.TrayIconBalloonTipTitle;
            this.trayIcon.BalloonTipText = text;
            this.trayIcon.ShowBalloonTip(10000);
            }

        }
    }

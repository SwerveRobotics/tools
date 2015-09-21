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
        SharedMemTaggedBlobQueue        bugBotMessageQueue;
        SharedMemTaggedBlobQueue        bugBotCommandQueue;
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
            this.statusText             = null;
            this.bugBotMessageQueue     = new SharedMemTaggedBlobQueue(false, TaggedBlob.BugBotMessageQueueUniquifier);
            this.bugBotCommandQueue     = new SharedMemTaggedBlobQueue(false, TaggedBlob.BugBotCommandQueueUniquifier);
            this.shutdownMonitor        = new ShutdownMonitor(Program.TrayUniquifier);
            this.shutdownMonitor.ShutdownEvent += (sender, e) => ShutdownApp();

            this.threadStarter = new HandshakeThreadStarter("Swerve Tray Notification Thread", this.NotificationThreadLoop);

            Application.ApplicationExit += (object sender, EventArgs e) => RemoveIcon();
            this.trayIcon.Visible = true;

            this.shutdownMonitor.StartMonitoring();
            StartBotBugNotificationThread();

            // Tell the service that we started
            try {
                this.bugBotCommandQueue.InitializeIfNecessary();
                this.bugBotCommandQueue.Write(new TaggedBlob(TaggedBlob.TagSwerveToolsTrayStarted, new byte[0]), 100);
                }
            catch (Exception)
                {
                // ignore
                }
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

            MenuItem menuItemExit   = new MenuItem(Resources.MenuItemExit, (sender, e) => ShutdownApp());
            MenuItem menuItemForget = new MenuItem(Resources.MenuItemForgetLastConnection, (sender, e) => ForgetLastConnection());
            this.trayIcon.ContextMenu = new ContextMenu(new MenuItem[] { menuItemForget, menuItemExit });
            }

        void UpdateIconText()
            {
            string title = $"{Resources.TrayIconText}";

            if (string.IsNullOrEmpty(this.statusText))
                this.trayIcon.Text = title;
            else
                {
                int cchMax      = 63;                // text has 63 char limit
                int cchOverhead = title.Length + 1;  // +1 for newline
                int cchStatus   = cchMax - cchOverhead;
                this.trayIcon.Text = $"{title}\n{this.statusText.SafeSubstring(0, cchStatus)}"; 
                }
            }

        void RemoveIcon()
            {
            // Remove this as robustly as we know how
            if (this.trayIcon != null)
                {
                this.trayIcon.Visible = false;
                this.trayIcon?.Dispose();
                }
            }

        void ShutdownApp()
            {
            RemoveIcon();
            ExitThread();
            }

        void ForgetLastConnection()
            {
            // Tell the server to forget about the last connected device
            try {
                Trace(Program.LoggingTag, "sending forget last connection...");
                this.bugBotCommandQueue.InitializeIfNecessary();
                this.bugBotCommandQueue.Write(new TaggedBlob(TaggedBlob.TagForgetLastConnection, new byte[0]), 1000);
                Trace(Program.LoggingTag, "...forget last connection sent");
                }
            catch (Exception)
                {
                // ignore, likely caused by service not yet started
                Trace(Program.LoggingTag, "...service not yet started");
                }
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
                this.trayIcon?.Dispose();           this.trayIcon = null;
                this.bugBotMessageQueue?.Dispose(); this.bugBotMessageQueue = null;
                this.bugBotCommandQueue?.Dispose(); this.bugBotCommandQueue = null;
                this.threadStarter?.Dispose();      this.threadStarter = null;
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
                starter.DoHandshake();

                // Spin, waiting for kernel to make the section for us
                for (bool thrown = true; !starter.StopRequested && thrown; )
                    {
                    try {
                        thrown = false;
                        this.bugBotMessageQueue.InitializeIfNecessary();
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
                        List<TaggedBlob> messages = this.bugBotMessageQueue.Read();
                        Trace(Program.LoggingTag, "...messages received");
                        if (messages.Count > 0)
                            {
                            // Separate the messages with newlines.
                            StringBuilder balloonText = new StringBuilder();
                            foreach (TaggedBlob blob in messages)
                                {
                                switch (blob.Tag)
                                    {
                                case TaggedBlob.TagBugBotMessage:
                                    if (balloonText.Length > 0)
                                        balloonText.Append("\n");
                                    balloonText.Append(blob.Message);
                                    break;
                                case TaggedBlob.TagBugBotStatus:
                                    // Update the status text
                                    this.statusText = blob.Message;
                                    UpdateIconText();
                                    break;
                                    }
                                }

                            // Display them to the user
                            if (balloonText.Length > 0)
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

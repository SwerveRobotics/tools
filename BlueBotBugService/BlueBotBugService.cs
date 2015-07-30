using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Org.SwerveRobotics.Tools.Library;

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    public partial class BlueBotBugService : DecompiledServiceBase, ITracer
        {
        //------------------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------------------
        
        System.Diagnostics.EventLog eventLog       = null;
        IntPtr                      hDeviceNotify  = IntPtr.Zero;

        private const string eventLogSourceName = "BlueBotBug";
        private const string eventLogName       = "Application";

        private Tools.Library.BlueBotBug library = null;

        //------------------------------------------------------------------------------------------
        // Construction
        //------------------------------------------------------------------------------------------

        public BlueBotBugService()
            {
            InitializeComponent();

            this.eventLog = new EventLog();
            if (!EventLog.SourceExists(eventLogSourceName))
                {
                EventLog.CreateEventSource(eventLogSourceName, eventLogName);
                }
            eventLog.Source = eventLogSourceName;
            eventLog.Log    = eventLogName;
            }

        //------------------------------------------------------------------------------------------
        // Notifications
        //------------------------------------------------------------------------------------------

        public static bool RunAsConsoleApp()
            {
            return Environment.UserInteractive;
            }

        internal void TestAsConsoleApp(string[] args)
        // Debugging hook per https://msdn.microsoft.com/en-us/library/7a50syb3(v=vs.110).aspx
        // We don't actually use this, as we still won't get 
            {
            this.OnStart(args);
            //
            // TODO: Put in message pump, convert device notification messages
            //
            System.Console.WriteLine("Press any key to stop...");
            while (!System.Console.KeyAvailable)
                {
                System.Threading.Thread.Yield();
                }
            //
            this.OnStop();
            }

        protected override void OnStart(string[] args)
            {
            this.Trace("starting");
            //
            if (RunAsConsoleApp())
                {
                }
            else
                {
                WIN32.DEV_BROADCAST_DEVICEINTERFACE filter = new WIN32.DEV_BROADCAST_DEVICEINTERFACE();
                filter.Initialize(System.Guid.Empty);

                this.hDeviceNotify = WIN32.RegisterDeviceNotification(this.ServiceHandle, filter, 
                    WIN32.DEVICE_NOTIFY_SERVICE_HANDLE | WIN32.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
                WIN32.ThrowIfFail(this.hDeviceNotify);
                }
            //
            this.library = new Tools.Library.BlueBotBug(this);
            this.library.Start();
            //
            this.Trace("started");
            }

        protected override void OnStop()
            {
            this.Trace("stopping");
            //
            if (this.library != null)
                { 
                library.Stop();
                library = null;
                }
            //
            if (RunAsConsoleApp())
                {
                }
            else
                {
                if (this.hDeviceNotify != IntPtr.Zero)
                    {
                    WIN32.UnregisterDeviceNotification(this.hDeviceNotify);
                    this.hDeviceNotify = IntPtr.Zero;
                    }
                }
            //
            this.Trace("stopped");
            }

        protected unsafe override bool ShouldDeferCustomCommandEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
            {
            bool result = true;
            switch (command)
                {
            case WIN32.SERVICE_CONTROL_DEVICEEVENT:
                WIN32.DEV_BROADCAST_HDR* pHeader = (WIN32.DEV_BROADCAST_HDR*)eventData;
                result = this.library.ShouldDeferDeviceEvent(eventType, pHeader);
                break;
                }
            return result;
            }

        protected unsafe override int OnCustomCommandEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
            {
            int result = WIN32.NO_ERROR;
            switch (command)
                {
            case WIN32.SERVICE_CONTROL_DEVICEEVENT:
                WIN32.DEV_BROADCAST_HDR* pHeader = (WIN32.DEV_BROADCAST_HDR*)eventData;
                result = this.library.OnDeviceEvent(eventType, pHeader);
                break;
                }
            return result;
            }

        //------------------------------------------------------------------------------------------
        // Tracing and exceptions
        //------------------------------------------------------------------------------------------

        void ITracer.Trace(string format, params object[] args)
            {
            this.Trace(format, args);
            }

        void Trace(string format, params object[] args)
            {
            Util.TraceDebug("BlueBotBug", format, args);
            if (RunAsConsoleApp())
                {
                Util.TraceStdOut("BlueBotBug", format, args);
                }
            }

        void Log(string format, params object[] args)
            {
            string message = String.Format(format, args);
            this.eventLog.WriteEntry(message);
            //
            this.Trace(format, args);
            }
        }
    }

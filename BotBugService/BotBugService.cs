using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    public partial class BotBugService : DecompiledServiceBase, ITracer, IDeviceEvents
        {
        //------------------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------------------
        
        System.Diagnostics.EventLog         eventLog       = null;

        private const string                eventLogSourceName = "BotBug";
        private const string                eventLogName       = "Application";
        public  const string                TraceTag           = "BotBug";

        private bool                        oleInitialized = false;
        private USBMonitor                  usbMonitor     = null;

        //------------------------------------------------------------------------------------------
        // Construction
        //------------------------------------------------------------------------------------------

        public BotBugService()
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

        void OleUninitialize()
            {
            if (this.oleInitialized)
                {
                WIN32.OleUninitialize();
                this.oleInitialized = false;
                }
            }

        protected override void HandleCommandCallbackPreHook(int command)
            {
            Trace($"HandleCommandCallback({command})");
            }

        //------------------------------------------------------------------------------------------
        // USB Notifications
        //------------------------------------------------------------------------------------------

        public readonly static Guid AndroidADBDeviceInterface = new Guid("{F72FE0D4-CBCB-407D-8814-9ED673D0DD6B}");

        protected override void OnStart(string[] args)
            {
            this.Trace("service is starting...");
            //
            try {
                WIN32.OleInitialize(IntPtr.Zero);
                this.oleInitialized = true;
                //
                this.usbMonitor = new USBMonitor(this, this, this.ServiceHandle, true);
                this.usbMonitor.AddDeviceInterfaceOfInterest(AndroidADBDeviceInterface);
                this.usbMonitor.Start();
                }
            catch (Exception e)
                {
                this.ExitCode = WIN32.ERROR_EXCEPTION_IN_SERVICE;
                this.ServiceSpecificExitCode = WIN32.Win32ErrorFromException(e);
                this.Trace($"exception: {e}");    
                throw;
                }
            //
            this.Trace("...service has started");
            }

        protected override void OnStop()
            {
            this.Trace("service is stopping...");
            //
            if (null != this.usbMonitor)
                {
                this.usbMonitor.Stop();
                this.usbMonitor.Dispose();
                this.usbMonitor = null;
                }
            this.OleUninitialize();
            //
            this.Trace("...service has stopped");
            }

        protected unsafe override bool ShouldDeferCustomCommandEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
            {
            bool result = true;
            switch (command)
                {
            case WIN32.SERVICE_CONTROL_DEVICEEVENT:
                WIN32.DEV_BROADCAST_HDR* pHeader = (WIN32.DEV_BROADCAST_HDR*)eventData;
                result = this.ShouldDeferDeviceEvent(eventType, pHeader);
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
                result = this.OnDeviceEvent(eventType, pHeader);
                break;
                }
            return result;
            }

        //-----------------------------------------------------------------------------------------
        // Device Events
        //-----------------------------------------------------------------------------------------

        public event EventHandler<DeviceEventArgs>       DeviceArrived;
        public event EventHandler<DeviceEventArgsCancel> DeviceQueryRemove;
        public event EventHandler<DeviceEventArgs>       DeviceQueryRemoveFailed;
        public event EventHandler<DeviceEventArgs>       DeviceRemovePending;
        public event EventHandler<DeviceEventArgs>       DeviceRemoveComplete;
        public event EventHandler<DeviceEventArgs>       DeviceTypeSpecific;
        public event EventHandler<DeviceEventArgs>       DeviceCustomEvent;
        public event EventHandler<DeviceEventArgs>       DeviceUserDefined;
        public event EventHandler<CancelEventArgs>       DeviceQueryChangeConfig;
        public event EventHandler<EventArgs>             DeviceConfigChanged;
        public event EventHandler<EventArgs>             DeviceConfigChangeCancelled;
        public event EventHandler<EventArgs>             DeviceDevNodesChanged;

        private unsafe void RaiseDeviceEvent(EventHandler<DeviceEventArgs> evt, WIN32.DEV_BROADCAST_HDR* pHeader)
            {
            if (evt != null)
                {
                DeviceEventArgs args = new DeviceEventArgs();
                args.pHeader = pHeader;
                evt(null, args);
                }
            }
        private unsafe void RaiseDeviceEvent(EventHandler<EventArgs> evt)
            {
            if (evt != null)
                {
                EventArgs args = new EventArgs();
                evt(null, args);
                }
            }
        private unsafe bool RaiseDeviceEvent(EventHandler<DeviceEventArgsCancel> evt, WIN32.DEV_BROADCAST_HDR* pHeader)
            {
            if (evt != null)
                {
                DeviceEventArgsCancel args = new DeviceEventArgsCancel();
                args.pHeader = pHeader;
                args.Cancel  = false;
                evt(null, args);
                return args.Cancel;
                }
            return false;
            }
        private bool RaiseDeviceEvent(EventHandler<CancelEventArgs> evt)
            {
            if (evt != null)
                {
                CancelEventArgs args = new CancelEventArgs();
                args.Cancel  = false;
                evt(null, args);
                return args.Cancel;
                }
            return false;
            }

        public unsafe bool ShouldDeferDeviceEvent(int eventType, WIN32.DEV_BROADCAST_HDR* pHeader)
            {
            // We basically can't defer any of these events, as the data pointed
            // to by pHeader will be invalid by the time we return, and so will be
            // junk in a deferred invocation.
            return false;
            }

        public unsafe int OnDeviceEvent(int eventType, WIN32.DEV_BROADCAST_HDR* pHeader)
            {
            int result = WIN32.NO_ERROR;
            bool cancel = false;

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363205(v=vs.85).aspx

            switch (eventType)
                {
            // pHeader is non-NULL
            case WIN32.DBT_DEVICEARRIVAL: 
                TraceEvent("device arrived: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceArrived, pHeader);
                break;
            case WIN32.DBT_DEVICEQUERYREMOVE: 
                TraceEvent("query device remove: {0}", pHeader->DeviceTypeName);   
                cancel = RaiseDeviceEvent(DeviceQueryRemove, pHeader);
                result = cancel ? WIN32.BROADCAST_QUERY_DENY : WIN32.TRUE;
                break;
            case WIN32.DBT_DEVICEQUERYREMOVEFAILED: 
                TraceEvent("query device remove failed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceQueryRemoveFailed, pHeader);
                break;
            case WIN32.DBT_DEVICEREMOVEPENDING: 
                TraceEvent("device remove pending: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceRemovePending, pHeader);
                break;
            case WIN32.DBT_DEVICEREMOVECOMPLETE: 
                TraceEvent("device remove complete: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceRemoveComplete, pHeader);
                break;

            case WIN32.DBT_DEVICETYPESPECIFIC: 
                TraceEvent("device type specific: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceTypeSpecific, pHeader);
                break;
            case WIN32.DBT_CUSTOMEVENT:
                TraceEvent("device custom: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceCustomEvent, pHeader);
                break;
            case WIN32.DBT_USERDEFINED:
                TraceEvent("device user defined: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceUserDefined, pHeader);
                break;

            // pHeader is NULL
            case WIN32.DBT_QUERYCHANGECONFIG:
                TraceEvent("device query change config: {0}", pHeader->DeviceTypeName);   
                cancel = RaiseDeviceEvent(DeviceQueryChangeConfig);
                result = cancel ? WIN32.BROADCAST_QUERY_DENY : WIN32.TRUE;
                break;
            case WIN32.DBT_CONFIGCHANGED:
                TraceEvent("device config changed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceConfigChanged);
                break;
            case WIN32.DBT_CONFIGCHANGECANCELED:
                TraceEvent("device config change cancelled: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceConfigChangeCancelled);
                break;
            case WIN32.DBT_DEVNODES_CHANGED:
                TraceEvent("device devnodes changed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceDevNodesChanged);
                break;

            default: 
                TraceEvent("unknown device event: {0}", eventType);
                break;
                }

            return result;
            }

        void TraceEvent(string format, params object[] arguments)
            {
            // Trace(format, arguments);
            }

        //------------------------------------------------------------------------------------------
        // Tracing and exceptions
        //------------------------------------------------------------------------------------------

        public void Trace(string format, params object[] args)
            {
            Util.TraceDebug(TraceTag, format, args);
            }

        public void Log(string format, params object[] args)
            {
            string message = string.Format(format, args);
            this.eventLog.WriteEntry(message);
            //
            this.Trace(format, args);
            }
        }
    }

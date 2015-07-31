using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Org.SwerveRobotics.Tools.Library
    {
    public interface ITracer
        { 
        void Trace(string format, params Object[] args);
        }

    public class BlueBotBug : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        private bool        oleInitialized = false;
        private bool        disposed       = false;
        private ITracer     tracer         = null;
        private USBMonitor  usbMonitor     = null;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

        public BlueBotBug(ITracer tracer)
            { 
            this.tracer = tracer;
            }

        ~BlueBotBug()
            {
            this.Dispose(false);
            }

        protected virtual void Dispose(bool fromUserCode)
            {
            if (!this.disposed)
                {    
                if (fromUserCode)
                    {
                    // Called from user's code. Can / should cleanup managed objects
                    }

                // Called from finalizers. Avoid referencing other objects
                this.OleUninitialize();
                }
            this.disposed = true;
            }

        void IDisposable.Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        void OleUninitialize()
            {
            if (this.oleInitialized)
                {
                WIN32.OleUninitialize();
                this.oleInitialized = false;
                }
            }

        //-----------------------------------------------------------------------------------------
        // Startup and shutdown
        //-----------------------------------------------------------------------------------------

        public void Start()
            {
            WIN32.OleInitialize(IntPtr.Zero);
            this.oleInitialized = true;
            //
            this.usbMonitor = new USBMonitor(this, this.tracer);
            this.usbMonitor.Start();
            }

        public void Stop()
            {
            if (null != this.usbMonitor)
                {
                this.usbMonitor.Stop();
                this.usbMonitor = null;
                }
            this.OleUninitialize();
            }

        //-----------------------------------------------------------------------------------------
        // Tracing
        //-----------------------------------------------------------------------------------------

        void Trace(string format, params object[] args)
            {
            this.tracer.Trace(format, args);
            }

        //-----------------------------------------------------------------------------------------
        // Events
        //-----------------------------------------------------------------------------------------

        public unsafe class DeviceEventArgs : EventArgs
            {
            public WIN32.DEV_BROADCAST_HDR* pHeader;
            }
        public unsafe class DeviceEventArgsCancel : DeviceEventArgs
            {
            public bool Cancel = false;
            }

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
            bool result = true;
            //
            switch (eventType)
                {
            case WIN32.DBT_QUERYCHANGECONFIG:
            case WIN32.DBT_DEVICEQUERYREMOVE: 
                result = false;
                break;
                }
            //
            return result;
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
                Trace("device arrived: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceArrived, pHeader);
                break;
            case WIN32.DBT_DEVICEQUERYREMOVE: 
                Trace("query device remove: {0}", pHeader->DeviceTypeName);   
                cancel = RaiseDeviceEvent(DeviceQueryRemove, pHeader);
                result = cancel ? WIN32.BROADCAST_QUERY_DENY : WIN32.TRUE;
                break;
            case WIN32.DBT_DEVICEQUERYREMOVEFAILED: 
                Trace("query device remove failed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceQueryRemoveFailed, pHeader);
                break;
            case WIN32.DBT_DEVICEREMOVEPENDING: 
                Trace("device remove pending: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceRemovePending, pHeader);
                break;
            case WIN32.DBT_DEVICEREMOVECOMPLETE: 
                Trace("device remove complete: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceRemoveComplete, pHeader);
                break;

            case WIN32.DBT_DEVICETYPESPECIFIC: 
                Trace("device type specific: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceTypeSpecific, pHeader);
                break;
            case WIN32.DBT_CUSTOMEVENT:
                Trace("device custom: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceCustomEvent, pHeader);
                break;
            case WIN32.DBT_USERDEFINED:
                Trace("device user defined: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceUserDefined, pHeader);
                break;

            // pHeader is NULL
            case WIN32.DBT_QUERYCHANGECONFIG:
                Trace("device query change config: {0}", pHeader->DeviceTypeName);   
                cancel = RaiseDeviceEvent(DeviceQueryChangeConfig);
                result = cancel ? WIN32.BROADCAST_QUERY_DENY : WIN32.TRUE;
                break;
            case WIN32.DBT_CONFIGCHANGED:
                Trace("device config changed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceConfigChanged);
                break;
            case WIN32.DBT_CONFIGCHANGECANCELED:
                Trace("device config change cancelled: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceConfigChangeCancelled);
                break;
            case WIN32.DBT_DEVNODES_CHANGED:
                Trace("device devnodes changed: {0}", pHeader->DeviceTypeName);   
                RaiseDeviceEvent(DeviceDevNodesChanged);
                break;

            default: 
                Trace("unknown device event: {0}", eventType);
                break;
                }

            return result;
            }

        }
    }

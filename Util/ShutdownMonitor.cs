using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class ShutdownMonitor : IDisposable
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------

        EventWaitHandle         shutdownRequestedEvent;
        Thread                  shutdownMonitorThread;
        ManualResetEventSlim    shutdownMonitorStartedEvent;
        bool                    stopRequested;
        bool                    disposed;

        public event EventHandler ShutdownEvent;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------
        
        public ShutdownMonitor(string uniquifier)
            {
            this.shutdownRequestedEvent      = new EventWaitHandle(false, EventResetMode.ManualReset, Util.GlobalName("ShutDownMonitor", uniquifier, "Event"));
            this.shutdownMonitorStartedEvent = new ManualResetEventSlim(false);   
            this.stopRequested               = false;
            this.shutdownMonitorThread       = null;
            this.disposed                    = false;
            }

        ~ShutdownMonitor()
            {
            Dispose(false);
            }

        public void Dispose()
            {
            Dispose(true);
            }
        
        protected virtual void Dispose(bool notFinalizer)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (notFinalizer)
                    {
                    lock (this)
                        {
                        StopMonitoring();
                        }
                    }
                this.shutdownRequestedEvent?.Dispose();
                this.shutdownMonitorStartedEvent?.Dispose();
                }
            }

        //----------------------------------------------------------------------------
        // Monitoring
        //----------------------------------------------------------------------------

        /** Shutdown all the instances in that are currently monitoring */
        public void RequestShutdown()
            {
            this.shutdownRequestedEvent.Set();
            }
        
        public void StartMonitoring()
            {
            lock (this)
                {
                StopMonitoring();
                this.stopRequested = false;

                this.shutdownMonitorThread = new Thread(ShutdownMonitorThread);
                this.shutdownMonitorThread.Name = "ShutdownMonitorThread";
                this.shutdownMonitorThread.Start();

                this.shutdownMonitorStartedEvent.Wait();
                }
            }

        public void StopMonitoring()
            {
            lock (this)
                {
                if (this.shutdownMonitorThread != null)
                    {
                    this.stopRequested = true;
                    this.shutdownMonitorThread.Interrupt();
                    this.shutdownMonitorThread.Join();
                    this.shutdownMonitorThread = null;
                    }
                }
            }

        void ShutdownMonitorThread()
            {
            this.shutdownMonitorStartedEvent.Set();
            while (!this.stopRequested)
                {
                try {
                    this.shutdownRequestedEvent.WaitOne();
                    this.ShutdownEvent?.Invoke(this, EventArgs.Empty) ;
                    return;
                    }
                catch (ThreadInterruptedException)
                    {
                    return;
                    }
                }
            }
        }
    }

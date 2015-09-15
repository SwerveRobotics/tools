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
        HandshakeThreadStarter  threadStarter;
        bool                    disposed;

        public event EventHandler ShutdownEvent;

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------
        
        public ShutdownMonitor(string uniquifier)
            {
            this.shutdownRequestedEvent = new EventWaitHandle(false, EventResetMode.ManualReset, Util.GlobalName("ShutDownMonitor", uniquifier, "Event"));
            this.threadStarter          = new HandshakeThreadStarter("ShutdownMonitorThread", ShutdownMonitorThread);
            this.disposed               = false;
            }

        ~ShutdownMonitor()
            {
            Dispose(false);
            }

        public void Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                this.shutdownRequestedEvent?.Dispose();     this.shutdownRequestedEvent = null;
                this.threadStarter?.Dispose();              this.threadStarter = null;
                }
            }

        //----------------------------------------------------------------------------
        // Monitoring
        //----------------------------------------------------------------------------

        /** Shutdown all the instances in that are currently monitoring */
        public void RequestShutdown()
            {
            // Ask those other guys to stop
            this.shutdownRequestedEvent.Set();

            // Hack wait a bit for the app to wake up and shut down. Ideally, we'd LIKE
            // to wait on its/their process handle(s), as that's the only way of being
            // assured that they've gone away, but for the moment at least it's not worth
            // the trouble it would take of doing that
            Thread.Sleep(200);
            }
        
        public void StartMonitoring()
            {
            lock (this)
                {
                StopMonitoring();
                this.threadStarter.Start();
                }
            }

        public void StopMonitoring()
            {
            lock (this)
                {
                this.threadStarter.Stop();
                }
            }

        void ShutdownMonitorThread(HandshakeThreadStarter starter)
            {
            starter.ThreadHasStarted();
            while (!starter.StopRequested)
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

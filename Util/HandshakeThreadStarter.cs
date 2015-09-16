using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class HandshakeThreadStarter : IDisposable
        {
        //------------------------------------------------------------------------------
        // State
        //------------------------------------------------------------------------------
        
        public bool                             StopRequested => this.stopRequested;
        public string                           Name           = null;

        private readonly ThreadStarterRunnable  runnable       = null;
        private ManualResetEventSlim            startedEvent   = null;
        private Thread                          thread         = null;
        private bool                            disposed       = false;
        private bool                            started        = false;
        private bool                            stopRequested  = false;

        //------------------------------------------------------------------------------
        // Construction
        //------------------------------------------------------------------------------
        
        public delegate void ThreadStarterRunnable(HandshakeThreadStarter obj);
        
        public HandshakeThreadStarter(string name, ThreadStarterRunnable start)
            {
            this.runnable     = start;
            this.Name         = name;
            this.thread       = null;
            this.startedEvent = new ManualResetEventSlim(false);
            }
        public HandshakeThreadStarter(ThreadStarterRunnable start) : this(null, start)
            {
            }

        ~HandshakeThreadStarter()
            {
            Dispose(false);
            }

        public void Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected virtual void Dispose(bool notFromFinalizer)
            {
            if (!this.disposed)
                {
                this.disposed = true;
                if (notFromFinalizer)
                    {
                    Stop();
                    }
                this.startedEvent?.Dispose();
                this.startedEvent = null;
                }
            }

        //------------------------------------------------------------------------------
        // Operations
        //------------------------------------------------------------------------------

        /**
         * Starts the thread going. Blocks until the thread actually runs and calls  
         * starter.ThreadIsRunning().  
         */
        public void Start()
            {
            lock (this)
                {
                Stop();
                this.startedEvent.Reset();
                this.stopRequested = false;
                this.thread = new Thread(Run);
                if (this.Name != null)
                    this.thread.Name = this.Name;
                this.thread.Start();
                this.startedEvent.Wait();
                this.started = true;
                }
            }

        public bool IsStarted { get { lock (this) { return this.started; } } }

        public void RequestStop()
            {
            lock (this)
                {
                this.stopRequested = true;
                // TODO: should we also interrupt here?
                }
            }

        /**
         *  Stops the thread, if currently running. Blocks until thread terminates.
         */
        public void Stop()
            {
            lock (this)
                {
                if (this.started)
                    {
                    try 
                        {
                        this.stopRequested = true;
                        this.thread.Interrupt();
                        this.thread.Join();     // may throw?
                        }
                    finally
                        {
                        this.thread  = null;
                        this.started = false;
                        }
                    }
                }
            }

        private void Run()
            {
            try {
                this.runnable(this);
                }
            catch (ThreadInterruptedException)
                {
                // ignore
                }
            }

        public void Handshake()
            {
            this.startedEvent.Set();
            }

        public void Join()
            {
            Thread thread;
            lock (this)
                {
                thread = this.thread;
                }
            thread?.Join();
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Org.SwerveRobotics.Tools.Util.Util;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class SingleInstance : IDisposable
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        Mutex  mutex;
        Mutex  probe;
        string uniquifier;
        bool   isFirstInstance;
        bool   disposed;

        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SingleInstance(string uniquifier)
            {
            this.uniquifier      = uniquifier;
            this.probe           = null;
            this.isFirstInstance = false;
            this.mutex           = null;

            // If there's another instance around who happens to be running under an incompatible 
            // user identity (like SYSTEM vs our 'user') then this may get an access denied. Annoying,
            // but also an indication that we're not the first instance! A bit of a hack, perhaps, but
            // not unreasonable.
            try {
                this.mutex = new Mutex(false, GlobalName("SingleInstance", this.uniquifier, "Mutex"));
                }
            catch (UnauthorizedAccessException)
                {
                }
            }

        ~SingleInstance()
            {
            Dispose(false);
            }

        public void Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected virtual void Dispose(bool fromUserCode)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (fromUserCode)
                    {
                    // Called from user's code. Can / should cleanup managed objects
                    }

                // Called from finalizers (and user code). Avoid referencing other objects
                this.probe?.Dispose();
                this.mutex?.Dispose();
                }
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        public bool IsFirstInstance()
            {
            if (this.mutex == null)
                return false;       // see comment in ctor

            if (this.mutex.WaitOneNoExcept()) 
                {
                try {
                    if (this.probe == null)
                        {
                        string probeName = GlobalName("SingleInstance", this.uniquifier, "Probe");
                        try {
                            this.probe = Mutex.OpenExisting(probeName);
                            this.isFirstInstance = false;
                            }
                        catch (WaitHandleCannotBeOpenedException)
                            {
                            this.probe = new Mutex(false, probeName);
                            this.isFirstInstance = true;
                            }
                        }
                    return this.isFirstInstance;
                    }
                finally
                    {
                    this.mutex.ReleaseMutex();
                    }
                }
    
            return true;    // err on safe side of having process run
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            this.mutex = new Mutex(false, SharedMemory.User($"SwerveToolsSingleInstance({uniquifier})Mutex"));
            this.probe = null;
            this.uniquifier = uniquifier;
            this.isFirstInstance = false;
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
            this.mutex.WaitOne();
            try {
                if (this.probe == null)
                    {
                    string probeName = SharedMemory.User($"SwerveToolsSingleInstance({this.uniquifier})Probe");
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
        }
    }

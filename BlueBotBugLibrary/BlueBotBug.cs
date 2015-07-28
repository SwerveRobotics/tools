using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Library
    {
    public class BlueBotBug : IDisposable
        {
        //-----------------------------------------------------------------------------------------
        // State
        //-----------------------------------------------------------------------------------------

        private bool oleInitialized = false;
        private bool disposed = false;

        //-----------------------------------------------------------------------------------------
        // Construction
        //-----------------------------------------------------------------------------------------

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
            }

        public void Stop()
            {
            this.OleUninitialize();
            }
        }
    }

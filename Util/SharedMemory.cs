using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;

namespace Org.SwerveRobotics.Tools.Util
    {
    /** A little utility that creates a shared-memory buffer for one process to read and another to write  */
    public abstract class SharedMemory : IDisposable
    // https://msdn.microsoft.com/EN-US/library/vstudio/dd267552(v=vs.100).aspx
    // TODO: Add security to this
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        protected Mutex                  mutex;
        protected EventWaitHandle        bufferChangedEvent;
        protected MemoryMappedFile       memoryMappedFile;
        protected MemoryMappedViewStream memoryViewStream;
        protected BinaryReader           reader;
        protected BinaryWriter           writer;
        private   bool                   disposed;
        
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------
        
        public static string Global(string name) => $"Global\\{name}";
        public static string User  (string name) => name;

        public SharedMemory(int cbBuffer, string uniquifier)
            {
            // Note: we rely on the fact that newly created memory is zeroed.
            // That makes the initial message count zero w/o us doing anything.
            this.mutex              = new Mutex(false, Global($"SwerveToolsSharedMem({uniquifier})Mutex"));
            this.bufferChangedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Global($"SwerveToolsSharedMem({uniquifier})Event"));
            this.memoryMappedFile   = MemoryMappedFile.CreateOrOpen(Global($"SwerveToolsSharedMem({uniquifier})Map"), cbBuffer);
            this.memoryViewStream   = this.memoryMappedFile.CreateViewStream(0, cbBuffer);
            this.reader             = new BinaryReader(memoryViewStream);
            this.writer             = new BinaryWriter(memoryViewStream);
            this.disposed           = false;
            }

        ~SharedMemory()
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

                // Called from finalizers (and user code). Avoid referencing other objects.
                this.reader?.Dispose();                 this.reader = null;
                this.writer?.Dispose();                 this.writer = null;
                this.memoryViewStream?.Dispose();       this.memoryViewStream = null;
                this.memoryMappedFile?.Dispose();       this.memoryMappedFile = null;
                }
            }
        }
    }

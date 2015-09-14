using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Org.SwerveRobotics.Tools.Util
    {
    /** A little utility that creates a shared-memory buffer for one process to read and another to write  */
    public abstract class SharedMemory : IDisposable
    // https://msdn.microsoft.com/EN-US/library/vstudio/dd267552(v=vs.100).aspx
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
        private   int                    cbBuffer;
        private   string                 uniquifier;
        private   bool                   create;
        
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SharedMemory(bool create, int cbBuffer, string uniquifier)
            {
            this.create             = create;
            this.disposed           = false;
            this.cbBuffer           = cbBuffer;
            this.uniquifier         = uniquifier;
            this.mutex              = null;
            this.bufferChangedEvent = null;
            this.memoryMappedFile   = null;
            this.memoryViewStream   = null;
            this.reader             = null;
            this.writer             = null;
            }

        /** Initialize this shared memory. In the non-create case, this may throw FileNotFoundException  
        /** if kernel hasn't created the memory section yet */
        public void Initialize()
            {
            bool createdNew; 
            
            this.mutex              = new Mutex
                                            (
                                            false, Util.GlobalName("SharedMem", this.uniquifier, "Mutex"), 
                                            out createdNew, Util.MutexSecurity()
                                            );

            this.bufferChangedEvent = new EventWaitHandle
                                            (
                                            false, 
                                            EventResetMode.AutoReset, Util.GlobalName("SharedMem", this.uniquifier, "Event"), 
                                            out createdNew, Util.EventSecurity()
                                            );

            string path = Util.GlobalName("SharedMem", this.uniquifier, "Map"); 
            this.memoryMappedFile   = create 
                                    ? MemoryMappedFile.CreateOrOpen
                                            (
                                            path, 
                                            this.cbBuffer, 
                                            MemoryMappedFileAccess.ReadWrite, 
                                            MemoryMappedFileOptions.None, Util.MapSecurity(create), 
                                            HandleInheritability.None
                                            )
                                     : MemoryMappedFile.OpenExisting(path, MemoryMappedFileRights.ReadWrite, HandleInheritability.None);

            this.memoryViewStream   = this.memoryMappedFile.CreateViewStream(0, this.cbBuffer);
            this.reader             = new BinaryReader(memoryViewStream);
            this.writer             = new BinaryWriter(memoryViewStream);
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

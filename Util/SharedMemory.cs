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
    /** A little utility that creates a shared-memory buffer useable by two parties */
    public class SharedMemory : IDisposable
    // https://msdn.microsoft.com/EN-US/library/vstudio/dd267552(v=vs.100).aspx
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        public  Mutex                  Mutex;
        public  EventWaitHandle        BufferChangedEvent;
        public  MemoryMappedFile       MemoryMappedFile;
        public  MemoryMappedViewStream MemoryViewStream;
        public  BinaryReader           Reader;
        public  BinaryWriter           Writer;

        private bool                   disposed;
        private int                    cbBuffer;
        private string                 uniquifier;
        private bool                   create;
        
        private readonly object        theLock;
        private bool                   initialized;

        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SharedMemory(bool create, int cbBuffer, string uniquifier)
            {
            this.theLock            = new object();
            this.initialized        = false;
            this.create             = create;
            this.disposed           = false;
            this.cbBuffer           = cbBuffer;
            this.uniquifier         = uniquifier;
            this.Mutex              = null;
            this.BufferChangedEvent = null;
            this.MemoryMappedFile   = null;
            this.MemoryViewStream   = null;
            this.Reader             = null;
            this.Writer             = null;
            }

        /** Initialize this shared memory. In the non-create case, this may throw FileNotFoundException  
        /** if kernel hasn't created the memory section yet */
        public void InitializeIfNecessary() // throws
            {
            lock (theLock)
                {
                if (!this.initialized)
                    {
                    try
                        {
                        bool createdNew; 
            
                        this.Mutex              = new Mutex
                                                        (
                                                        false, Util.GlobalName("SharedMem", this.uniquifier, "Mutex"), 
                                                        out createdNew, Util.MutexSecurity()
                                                        );

                        this.BufferChangedEvent = new EventWaitHandle
                                                        (
                                                        false, 
                                                        EventResetMode.AutoReset, Util.GlobalName("SharedMem", this.uniquifier, "Event"), 
                                                        out createdNew, Util.EventSecurity()
                                                        );

                        string path = Util.GlobalName("SharedMem", this.uniquifier, "Map"); 
                        this.MemoryMappedFile   = create 
                                                ? MemoryMappedFile.CreateOrOpen
                                                        (
                                                        path, 
                                                        this.cbBuffer, 
                                                        MemoryMappedFileAccess.ReadWrite, 
                                                        MemoryMappedFileOptions.None, Util.MapSecurity(create), 
                                                        HandleInheritability.None
                                                        )
                                                 : MemoryMappedFile.OpenExisting(path, MemoryMappedFileRights.ReadWrite, HandleInheritability.None);

                        this.MemoryViewStream   = this.MemoryMappedFile.CreateViewStream(0, this.cbBuffer);
                        this.Reader             = new BinaryReader(this.MemoryViewStream);
                        this.Writer             = new BinaryWriter(this.MemoryViewStream);

                        this.initialized = true;
                        }
                    catch (Exception)
                        {
                        // Don't be partially initialized
                        Close();
                        throw;
                        }
                    }
                }
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

        protected virtual void Dispose(bool notFromFinalizer)
            {
            if (!disposed)
                {
                this.disposed = true;
                if (notFromFinalizer)
                    {
                    }
                Close();
                }
            }

        private void Close()
            {
            this.Reader?.Dispose();                 this.Reader = null;
            this.Writer?.Dispose();                 this.Writer = null;
            this.MemoryViewStream?.Dispose();       this.MemoryViewStream = null;
            this.MemoryMappedFile?.Dispose();       this.MemoryMappedFile = null;
            this.Mutex?.Dispose();                  this.Mutex = null;
            this.BufferChangedEvent?.Dispose();     this.BufferChangedEvent = null;
            }
        }
    }

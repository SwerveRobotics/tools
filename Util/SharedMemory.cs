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
        
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------
        
        public static string Global(string name) => $"Global\\{name}";
        public static string User  (string name) => name;

        public SharedMemory(int cbBuffer, string uniquifier)
            {
            bool createdNew; 

            this.mutex              = new Mutex
                                            (
                                            false, 
                                            Global($"SwerveToolsSharedMem({uniquifier})Mutex"), 
                                            out createdNew, 
                                            MutexSecurity()
                                            );
            this.bufferChangedEvent = new EventWaitHandle
                                            (
                                            false, 
                                            EventResetMode.AutoReset, 
                                            Global($"SwerveToolsSharedMem({uniquifier})Event"), 
                                            out createdNew, 
                                            EventSecurity()
                                            );
            this.memoryMappedFile   = MemoryMappedFile.CreateOrOpen
                                            (
                                            Global($"SwerveToolsSharedMem({uniquifier})Map"), 
                                            cbBuffer, 
                                            MemoryMappedFileAccess.ReadWrite, 
                                            MemoryMappedFileOptions.None, 
                                            MapSecurity(), 
                                            HandleInheritability.None
                                            );
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

        //---------------------------------------------------------------------------------------
        // Utility
        //---------------------------------------------------------------------------------------
        
        SecurityIdentifier GetEveryone()
            {
            return new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            }

        MutexSecurity MutexSecurity()
            {
            SecurityIdentifier user = GetEveryone();
            MutexSecurity result = new MutexSecurity();

            MutexAccessRule rule = new MutexAccessRule(user, MutexRights.Synchronize | MutexRights.Modify | MutexRights.Delete, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }

        EventWaitHandleSecurity EventSecurity()
            {
            SecurityIdentifier user = GetEveryone();
            EventWaitHandleSecurity result = new EventWaitHandleSecurity();

            EventWaitHandleAccessRule  rule = new EventWaitHandleAccessRule(user, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify | EventWaitHandleRights.Delete, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }

        MemoryMappedFileSecurity MapSecurity()
            {
            SecurityIdentifier user = GetEveryone();
            MemoryMappedFileSecurity result = new MemoryMappedFileSecurity();

            AccessRule<MemoryMappedFileRights> rule = new AccessRule<MemoryMappedFileRights>(user, MemoryMappedFileRights.ReadWrite|MemoryMappedFileRights.Delete, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }
        }
    }

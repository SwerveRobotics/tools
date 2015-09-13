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
    public class BotBugSharedMemory : IDisposable
    // https://msdn.microsoft.com/EN-US/library/vstudio/dd267552(v=vs.100).aspx
    // TODO: Add security to this
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        int                    messageCount;
        Mutex                  mutex;
        EventWaitHandle        bufferChangedEvent;
        MemoryMappedFile       memoryMappedFile;
        MemoryMappedViewStream memoryViewStream;
        
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public BotBugSharedMemory()
            {
            const int cbBuffer = 1024;
            this.messageCount       = 0;
            this.mutex              = new Mutex(false, "SwerveBotBugMutex");
            this.bufferChangedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "SwerveBotBugEvent");
            this.memoryMappedFile   = MemoryMappedFile.CreateOrOpen("SwerveBotBugMemoryMap", cbBuffer);
            this.memoryViewStream   = this.memoryMappedFile.CreateViewStream(0, cbBuffer);
            }

        void IDisposable.Dispose()
            {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            }

        public virtual void Dispose(bool fromUserCode)
            {
            if (fromUserCode)
                {
                // Called from user's code. Can / should cleanup managed objects
                }

            // Called from finalizers (and user code). Avoid referencing other objects.
            this.memoryViewStream?.Dispose();       this.memoryViewStream = null;
            this.memoryMappedFile?.Dispose();       this.memoryMappedFile = null;
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        public void Write(string message)
            {
            this.mutex.WaitOne();
            try {
                this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                BinaryWriter writer = new BinaryWriter(memoryViewStream);
                writer.Write(++this.messageCount);
                writer.Write(message);
                //
                this.bufferChangedEvent.Set();
                }
            finally
                {
                this.mutex.ReleaseMutex();
                }
            }

        public string ReadString()
            {
            for (;;)
                {
                // Wait until there's (probably) something new
                this.bufferChangedEvent.WaitOne();

                // Read it
                this.mutex.WaitOne();
                try {
                    this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                    BinaryReader reader = new BinaryReader(memoryViewStream);
                    int messageNumber = reader.ReadInt32();
                    string message    = reader.ReadString();

                    // If it's a message we haven't seen before, then remember it
                    if (messageNumber > this.messageCount)
                        {
                        this.messageCount = messageNumber;
                        return message;
                        }
                    }
                finally
                    {
                    this.mutex.ReleaseMutex();
                    }
                }
            }
        }
    }

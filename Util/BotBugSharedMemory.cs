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

        Mutex                  mutex;
        EventWaitHandle        bufferChangedEvent;
        MemoryMappedFile       memoryMappedFile;
        MemoryMappedViewStream memoryViewStream;
        BinaryReader           reader;
        BinaryWriter           writer;
        
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public BotBugSharedMemory()
            {
            // Note: we rely on the fact that newly created memory is zeroed.
            // That makes the initial message count zero w/o us doing anything.
            const int cbBuffer = 2048;
            this.mutex              = new Mutex(false, "SwerveBotBugMutex");
            this.bufferChangedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "SwerveBotBugEvent");
            this.memoryMappedFile   = MemoryMappedFile.CreateOrOpen("SwerveBotBugMemoryMap", cbBuffer);
            this.memoryViewStream   = this.memoryMappedFile.CreateViewStream(0, cbBuffer);
            this.reader             = new BinaryReader(memoryViewStream);
            this.writer             = new BinaryWriter(memoryViewStream);
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
            this.reader?.Dispose();
            this.writer?.Dispose();
            this.memoryViewStream?.Dispose();       this.memoryViewStream = null;
            this.memoryMappedFile?.Dispose();       this.memoryMappedFile = null;
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        /** Append a new message to the queue of messsages */ 
        public void Write(string message)
            {
            this.mutex.WaitOne();
            try {
                // Read the message count at the start of the buffer
                this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                int messageCount = reader.ReadInt32();

                // Skip over that many messages
                for (int i = 0; i < messageCount; i++)
                    {
                    reader.ReadString();
                    }
                
                // Write the next string. 'May hit the buffer end and throw exception, but 
                // that's ok; we'll just be ignoring this message
                writer.Write(message); 

                // Update the message count
                this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                writer.Write(messageCount + 1);
                
                // Let the reader know there's new stuff
                this.bufferChangedEvent.Set();
                }
            catch (Exception)
                {
                // Ignore write errors; they'll be at buffer end. The actual exeption we 
                // see is a NotSupportedException thrown by the stream when asked to extend
                // it's length
                }
            finally
                {
                this.mutex.ReleaseMutex();
                }
            }

        /** Read all the messages in the queue */
        public List<string> Read()
            {
            List<string> result = new List<string>();

            // Wait until there's (probably) something new
            this.bufferChangedEvent.WaitOne();

            this.mutex.WaitOne();
            try {
                // Read the message count at the start of the buffer
                this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                int messageCount = reader.ReadInt32();

                // Read over that many messages
                for (int i = 0; i < messageCount; i++)
                    {
                    result.Add(reader.ReadString());
                    }

                // Update the message count
                this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                writer.Write((int)0);
                }
            finally
                {
                this.mutex.ReleaseMutex();
                }

            return result;
            }
        }
    }

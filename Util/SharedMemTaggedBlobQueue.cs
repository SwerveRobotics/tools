using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.SwerveRobotics.Tools.Util.Util;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class TaggedBlob
        {
        //---------------------------------------------------------------------
        // Defined constants (could generalize this better)
        //---------------------------------------------------------------------

        public const int TagBugBotMessage        = 1;
        public const int TagBugBotStatus         = 2;
        public const int TagForgetLastConnection = 3;
        public const int TagSwerveToolsTrayStarted = 4;

        public const string BugBotMessageQueueUniquifier = "BugBotMessageQueue";
        public const string BugBotCommandQueueUniquifier = "BogBotCommandQueue";

        //---------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------

        public  int     Tag;
        public  byte[]  Payload;
        public  string  Message { get { return StringFromByteArray(this.Payload); } }

        //---------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------

        public TaggedBlob(int tag, string payload) : this(tag, ByteArrayFromString(payload))
            {
            }
        public TaggedBlob(int tag, byte[] payload)
            {
            this.Tag     = tag;
            this.Payload = payload ?? new byte[0];
            }
        public TaggedBlob()
            {
            }

        //---------------------------------------------------------------------
        // Reading and writing
        //---------------------------------------------------------------------

        public static TaggedBlob Read(BinaryReader reader)
            {
            TaggedBlob result = new TaggedBlob();
            result.Tag     = reader.ReadInt32();
            int cb         = reader.ReadInt32();
            result.Payload = reader.ReadBytes(cb);
            return result;
            }
        public void Write(BinaryWriter writer)
            {
            writer.Write(this.Tag);
            writer.Write(this.Payload.Length);
            writer.Write(this.Payload);
            }

        static string StringFromByteArray(byte [] bytes)
            {
            return Encoding.UTF8.GetString(bytes);
            }
        static byte[] ByteArrayFromString(string s)
            {
            return Encoding.UTF8.GetBytes(s);
            }
        }

    public class SharedMemTaggedBlobQueue : IDisposable
        {
        //---------------------------------------------------------------------------------------
        // State
        //---------------------------------------------------------------------------------------

        bool         disposed;
        SharedMemory sharedMemory;

        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SharedMemTaggedBlobQueue(bool create, string uniquifier)
            {
            // Note: we rely on the fact that newly created memory is zeroed.
            // That makes the initial message count zero w/o us doing anything.
            this.sharedMemory = new SharedMemory(create, 2048, $"StringQueue({uniquifier})");
            this.disposed = false;
            }

        public void InitializeIfNecessary() // throws
            {
            this.sharedMemory.InitializeIfNecessary();
            }

        ~SharedMemTaggedBlobQueue()
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
                this.sharedMemory?.Dispose();
                }
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        /** Append a new message to the queue of messsages */ 
        public void Write(int tag, string message, int msTimeout=-1)
            {
            Write(new TaggedBlob(tag, message), msTimeout);
            }

        public void Write(TaggedBlob blob, int msTimeout=-1)
            {
            if (this.sharedMemory.Mutex.WaitOneNoExcept(msTimeout)) 
                {
                try {
                    // Read the message count at the start of the buffer
                    this.sharedMemory.MemoryViewStream.Seek(0, SeekOrigin.Begin);
                    int messageCount = this.sharedMemory.Reader.ReadInt32();

                    // Skip over that many messages
                    for (int i = 0; i < messageCount; i++)
                        {
                        TaggedBlob.Read(this.sharedMemory.Reader);
                        }
                
                    // Write the next string. 'May hit the buffer end and throw exception, but 
                    // that's ok; we'll just be ignoring this message
                    blob.Write(this.sharedMemory.Writer);

                    // Update the message count
                    this.sharedMemory.MemoryViewStream.Seek(0, SeekOrigin.Begin);
                    this.sharedMemory.Writer.Write(messageCount + 1);
                
                    // Let the reader know there's new stuff
                    this.sharedMemory.BufferChangedEvent.Set();
                    }
                catch (Exception)
                    {
                    // Ignore write errors; they'll be at buffer end. The actual exeption we 
                    // see is a NotSupportedException thrown by the stream when asked to extend
                    // it's length
                    }
                finally
                    {
                    this.sharedMemory.Mutex.ReleaseMutex();
                    }
                }
            }

        /** Read all the messages in the queue */
        public List<TaggedBlob> Read()
            {
            List<TaggedBlob> result = new List<TaggedBlob>();

            // Wait until there's (probably) something new
            this.sharedMemory.BufferChangedEvent.WaitOne();

            if (this.sharedMemory.Mutex.WaitOneNoExcept()) 
                {
                try {
                    // Read the message count at the start of the buffer
                    this.sharedMemory.MemoryViewStream.Seek(0, SeekOrigin.Begin);
                    int messageCount = this.sharedMemory.Reader.ReadInt32();

                    // Read over that many messages
                    for (int i = 0; i < messageCount; i++)
                        {
                        result.Add(TaggedBlob.Read(this.sharedMemory.Reader));
                        }

                    // Update the message count
                    this.sharedMemory.MemoryViewStream.Seek(0, SeekOrigin.Begin);
                    this.sharedMemory.Writer.Write((int)0);
                    }
                finally
                    {
                    this.sharedMemory.Mutex.ReleaseMutex();
                    }
                }
            return result;
            }
        }
    }

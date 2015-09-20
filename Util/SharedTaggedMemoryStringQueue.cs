using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.SwerveRobotics.Tools.Util.Util;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class TaggedMessage
        {
        public  int     Tag;
        public  string  Message;

        public const int TagMessage = 1;
        public const int TagStatus  = 2;
        }

    public class SharedTaggedMemoryStringQueue : SharedMemory
        {
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SharedTaggedMemoryStringQueue(bool create, string uniquifier) : base(create, 2048, $"StringQueue({uniquifier})")
            {
            // Note: we rely on the fact that newly created memory is zeroed.
            // That makes the initial message count zero w/o us doing anything.
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        TaggedMessage ReadTaggedMessage()
            {
            TaggedMessage result = new TaggedMessage();
            result.Tag     = reader.ReadInt32();
            result.Message = reader.ReadString();
            return result;
            }

        /** Append a new message to the queue of messsages */ 
        public void Write(int tag, string message, int msTimeout=-1)
            {
            if (this.mutex.WaitOneNoExcept(msTimeout)) 
                {
                try {
                    // Read the message count at the start of the buffer
                    this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                    int messageCount = reader.ReadInt32();

                    // Skip over that many messages
                    for (int i = 0; i < messageCount; i++)
                        {
                        ReadTaggedMessage();
                        }
                
                    // Write the next string. 'May hit the buffer end and throw exception, but 
                    // that's ok; we'll just be ignoring this message
                    writer.Write(tag);
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
            }

        /** Read all the messages in the queue */
        public List<TaggedMessage> Read()
            {
            List<TaggedMessage> result = new List<TaggedMessage>();

            // Wait until there's (probably) something new
            this.bufferChangedEvent.WaitOne();

            if (this.mutex.WaitOneNoExcept()) 
                {
                try {
                    // Read the message count at the start of the buffer
                    this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                    int messageCount = reader.ReadInt32();

                    // Read over that many messages
                    for (int i = 0; i < messageCount; i++)
                        {
                        result.Add(ReadTaggedMessage());
                        }

                    // Update the message count
                    this.memoryViewStream.Seek(0, SeekOrigin.Begin);
                    writer.Write((int)0);
                    }
                finally
                    {
                    this.mutex.ReleaseMutex();
                    }
                }
            return result;
            }
        }
    }

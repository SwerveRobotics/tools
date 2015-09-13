using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    public class SharedMemoryStringQueue : SharedMemory
        {
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------

        public SharedMemoryStringQueue(string uniquifier) : base(2048, $"StringQueue({uniquifier})")
            {
            // Note: we rely on the fact that newly created memory is zeroed.
            // That makes the initial message count zero w/o us doing anything.
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

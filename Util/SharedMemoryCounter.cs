using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    class SharedMemoryCounter : SharedMemory
        {
        //---------------------------------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------------------------------
        
        public SharedMemoryCounter(bool create, string uniquifier) : base(create, 4, $"Counter({uniquifier})")
            {
            }

        //---------------------------------------------------------------------------------------
        // Operations
        //---------------------------------------------------------------------------------------

        public void Increment()
            {
            Adjust(1);
            }
        public void Decrement()
            {
            Adjust(-1);
            }
        void Adjust(int delta)
            {
            this.mutex.WaitOne();
            try {
                this.memoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                int count = this.reader.ReadInt32(); 

                this.memoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                this.writer.Write(count + delta);
                }
            finally
                {
                this.mutex.ReleaseMutex();
                }
            }
        public int Read()
            {
            this.mutex.WaitOne();
            try {
                this.memoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                return this.reader.ReadInt32(); 
                }
            finally
                {
                this.mutex.ReleaseMutex();
                }
            }
        }
    }

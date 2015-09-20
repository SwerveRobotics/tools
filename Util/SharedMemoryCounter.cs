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
            this.Mutex.WaitOne();
            try {
                this.MemoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                int count = this.Reader.ReadInt32(); 

                this.MemoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                this.Writer.Write(count + delta);
                }
            finally
                {
                this.Mutex.ReleaseMutex();
                }
            }
        public int Read()
            {
            this.Mutex.WaitOne();
            try {
                this.MemoryViewStream.Seek(0, System.IO.SeekOrigin.Begin);
                return this.Reader.ReadInt32(); 
                }
            finally
                {
                this.Mutex.ReleaseMutex();
                }
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    static class Util
        {
        public static bool WaitOneNoExcept(this Mutex mutex, int msTimeout = -1)
            {
            try {
                return mutex.WaitOne(msTimeout);
                }
            catch (Exception)
                {
                return false;
                }
            }
        }
    }

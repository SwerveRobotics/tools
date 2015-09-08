using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managed.Adb
    {
    static class Util
        {
        public static bool equalsIgnoreCase(string me, string him)
            {
            return me.ToLowerInvariant() == him.ToLowerInvariant();
            }
        public static bool equals(string me, string him)
            {
            return me == him;
            }
        }
    }

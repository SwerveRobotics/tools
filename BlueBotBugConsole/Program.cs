using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.SwerveRobotics.Tools.Library;

namespace Org.SwerveRobotics.BlueBotBug.Console
    {
    class Program : ITracer
        {
        void DoMain(string[] args)
            {
            Tools.Library.BlueBotBug bluebotbug = new Tools.Library.BlueBotBug(this);
            //
            bluebotbug.Start();
            //
            System.Console.WriteLine("Press any key to stop...");
            while (!System.Console.KeyAvailable)
                {
                System.Threading.Thread.Yield();
                }
            //
            bluebotbug.Stop();
            }

        static void Main(string[] args)
            {
            (new Program()).DoMain(args);
            }

        //------------------------------------------------------------------------------------------
        // Tracing
        //------------------------------------------------------------------------------------------

        void ITracer.Trace(string format, params object[] args)
            {
            Util.TraceDebug("BlueBotBug console", format, args);
            }
        }
    }

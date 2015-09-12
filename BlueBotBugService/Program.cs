using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

// https://msdn.microsoft.com/en-us/library/zt39148a(v=vs.110).aspx

namespace Org.SwerveRobotics.BotBug.Service
    {
    static class Program
        {
        static void Main(string[] args)
            {
            DecompiledServiceBase[] ServicesToRun = new DecompiledServiceBase[] 
                { 
                new BotBugService() 
                };
            DecompiledServiceBase.Run(ServicesToRun);
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

// https://msdn.microsoft.com/en-us/library/zt39148a(v=vs.110).aspx

namespace Org.SwerveRobotics.BlueBotBug.Service
    {
    static class Program
        {
        static void Main(string[] args)
            {
            if (BlueBotBugService.RunAsConsoleApp())
                {
                BlueBotBugService service = new BlueBotBugService();
                service.TestAsConsoleApp(args);
                }
            else
                {
                DecompiledServiceBase[] ServicesToRun = new DecompiledServiceBase[] 
                    { 
                    new BlueBotBugService() 
                    };
                DecompiledServiceBase.Run(ServicesToRun);
                }
            }
        }
    }

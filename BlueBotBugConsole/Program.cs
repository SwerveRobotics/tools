using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.SwerveRobotics.BlueBotBug;

namespace Org.SwerveRobotics.BlueBotBug.Console
    {
    class Program
        {
        static void Main(string[] args)
            {
            Library.Main library = new Library.Main();
            //
            library.Start();
            //
            System.Console.WriteLine("Press any key to stop...");
            while (!System.Console.KeyAvailable)
                {
                System.Threading.Thread.Yield();
                }
            //
            library.Stop();
            }
        }
    }

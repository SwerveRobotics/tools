using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.SwerveRobotics.Tools.Util;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    static class Program
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------
        
        public static string LoggingTag = "BotBugTray";

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        [STAThread]
        static void Main()
            {
            SingleInstance singleInstance = new SingleInstance("SwerveToolsTray");
            if (singleInstance.IsFirstInstance())
                {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
                }
            }
        }
    }

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
        
        public static string LoggingTag     = "BotBugTray";
        public static string TrayUniquifier = "SwerveToolsTray";

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        [STAThread]
        static void Main()
            {
            Util.Util.Trace(LoggingTag, "starting SwerveToolsTray...");
            //
            SingleInstance singleInstance = new SingleInstance(TrayUniquifier);
            if (singleInstance.IsFirstInstance())
                {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
                }
            //
            Util.Util.Trace(LoggingTag, "...exiting SwerveToolsTray");
            }
        }
    }

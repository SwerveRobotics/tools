using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.SwerveRobotics.Tools.Util;
using static Org.SwerveRobotics.Tools.Util.Util;

namespace Org.SwerveRobotics.Tools.SwerveToolsTray
    {
    static class Program
        {
        //----------------------------------------------------------------------------
        // State
        //----------------------------------------------------------------------------
        
        public static string LoggingTag     = "BotBug: tray";
        public static string TrayUniquifier = "SwerveToolsTray";

        //----------------------------------------------------------------------------
        // Construction
        //----------------------------------------------------------------------------

        [STAThread]
        static void Main()
            {
            Trace(LoggingTag, "SwerveToolsTray starting...");
            //
            SingleInstance singleInstance = new SingleInstance(TrayUniquifier);
            if (singleInstance.IsFirstInstance())
                {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
                }
            else
                {
                Trace(LoggingTag, "not first instance");
                }
            //
            Trace(LoggingTag, "...exiting SwerveToolsTray");
            }
        }
    }

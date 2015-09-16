//
// PostBuildEvent.js
//
// Macros we could pass to the command line
// https://msdn.microsoft.com/en-us/library/c02as0cs.aspx

// Constant values from Windows Installer
var msiOpenDatabaseModeTransact = 1;

var msiViewModifyInsert = 1;
var msiViewModifyUpdate = 2;
var msiViewModifyAssign = 3;
var msiViewModifyReplace = 4;
var msiViewModifyDelete = 6;

var msidbCustomActionTypeRollback       = 0x00000100;
var msidbCustomActionTypeCommit         = 0x00000200;
var msidbCustomActionTypeInScript       = 0x00000400;
var msidbCustomActionTypeNoImpersonate  = 0x00000800;

// We're provided with the path to the .msi we're building.
// We assume that this is in the same directory as all the other
// build outputs
var msiPath = WScript.Arguments(0);
var outDir = msiPath.substr(0, msiPath.lastIndexOf("\\")+1);
var trayLauncherFile = "SwerveToolsSetupTrayLauncher.dll";
var trayLauncherPath = outDir + trayLauncherFile;

WScript.StdErr.WriteLine(WScript.ScriptName + ": processing '" + msiPath + "' with '" + trayLauncherPath + "'");

try {
    //----------------------------------------------------------------------------------------------------------------------
    // Open the database
    //----------------------------------------------------------------------------------------------------------------------

    // Documentation on WindowsInstaller.Installer interface
    // https://msdn.microsoft.com/en-us/library/aa369432(v=vs.85).aspx
    var installer = WScript.CreateObject("WindowsInstaller.Installer");
    var database = installer.OpenDatabase(msiPath, msiOpenDatabaseModeTransact);

    //----------------------------------------------------------------------------------------------------------------------
    // Disable both the modify and repair options in Add/Remove programs
    //----------------------------------------------------------------------------------------------------------------------

    // SQL syntax
    // https://msdn.microsoft.com/en-us/library/aa372021(v=vs.85).aspx
    // https://msdn.microsoft.com/en-us/library/aa368562(v=vs.85).aspx
    // https://github.com/Excel-DNA/WiXInstaller/blob/master/Source/WiRunSQL.vbs
    // https://msdn.microsoft.com/en-us/library/aa367523(v=vs.85).aspx              adding binary data

    var view;
    view = database.OpenView("INSERT INTO `Property` (`Property`.`Property`, `Property`.`Value`) VALUES ('ARPNOMODIFY', '1')");
    view.Execute();

    view = database.OpenView("INSERT INTO `Property` (`Property`.`Property`, `Property`.`Value`) VALUES ('ARPNOREPAIR', '1')");
    view.Execute();

    //----------------------------------------------------------------------------------------------------------------------
    // Remove 'repair' from the maintenance dialog
    // 
    // This is from
    //  http://stackoverflow.com/questions/819722/remove-repair-option-screen-from-msi-installer
    // but adapted and with bugs fixed
    //----------------------------------------------------------------------------------------------------------------------
    
    view = database.OpenView("UPDATE `Property` SET `Property`.`Value`='Remove' WHERE `Property`.`Property`='MaintenanceForm_Action'");
    view.Execute();

    view = database.OpenView("UPDATE `Control` SET `Control`.`Text`='{\\VSI_MS_Sans_Serif13.0_0_0}Select \"Finish\" to remove [ProductName]' WHERE `Control`.`Dialog_`='MaintenanceForm' AND `Control`.`Control`='BodyText'");
    view.Execute();

    view = database.OpenView("UPDATE `Control` SET `Control`.`Control_Next`='CancelButton' WHERE `Control`.`Dialog_`='MaintenanceForm' AND `Control`.`Control`='FinishButton'");
    view.Execute();

    view = database.OpenView("DELETE FROM `Control` WHERE `Control`.`Dialog_`='MaintenanceForm' AND `Control`.`Control`='RepairRadioGroup'");
    view.Execute();

    //----------------------------------------------------------------------------------------------------------------------
    // Add custom action to launch tray on setup exit
    // https://msdn.microsoft.com/en-us/library/aa367527(v=vs.85).aspx
    // https://msdn.microsoft.com/en-us/library/aa367521(v=vs.85).aspx
    //----------------------------------------------------------------------------------------------------------------------

    // Insert our custom action DLL as a binary blob in the MSI
    var record = installer.CreateRecord(1);
    record.SetStream(1, trayLauncherPath);
    view = database.OpenView("INSERT INTO `Binary` (`Name`, `Data`) VALUES ('"+ trayLauncherFile +"', ?)");
    view.Execute(record);

    // Create a custom action that references that blob
    view = database.OpenView("INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('Launch', '65', '" + trayLauncherFile + "', 'LaunchTray')");
    view.Execute();

    // Run that custom action when the user closes the final dialog
    view = database.OpenView("INSERT INTO `ControlEvent` (`Dialog_`, `Control_`, `Event`, `Argument`, `Condition`, `Ordering`) VALUES ('FinishedForm', 'CloseButton', 'DoAction', 'Launch', 'NOT Installed', '1')");
    view.Execute();

    //----------------------------------------------------------------------------------------------------------------------
    // Exit and commit
    //----------------------------------------------------------------------------------------------------------------------

    view.Close();
	database.Commit();
    }
catch(e)
    {
    WScript.StdErr.WriteLine("exception thrown: " + e + ": message='" + e.message + "' stack=" + e.stack);
	WScript.Quit(1);
    }

WScript.StdOut.WriteLine(WScript.ScriptName + ": done");




// Notes/examples we might need in the future:
// 
//try {
//    sql = "SELECT `Action`, `Type`, `Source`, `Target` FROM `CustomAction`";
//    view = database.OpenView(sql);
//    view.Execute();
//    record = view.Fetch();
//    while (record) {
//        if (record.IntegerData(2) & msidbCustomActionTypeInScript) {
//            record.IntegerData(2) = record.IntegerData(2) | msidbCustomActionTypeNoImpersonate;
//            view.Modify(msiViewModifyReplace, record);
//        }
//        record = view.Fetch();
//    }

//    view.Close();
//    database.Commit();
//}
//catch (e) {
//    WScript.StdErr.WriteLine(e);
//    WScript.Quit(1);
//}

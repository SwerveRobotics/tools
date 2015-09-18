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

// from msidefs.h
var msidbCustomActionTypeDll              = 0x00000001;  // Target = entry point name
var msidbCustomActionTypeExe              = 0x00000002;  // Target = command line args
var msidbCustomActionTypeTextData         = 0x00000003;  // Target = text string to be formatted and set into property
var msidbCustomActionTypeJScript          = 0x00000005;  // Target = entry point name; null if none to call
var msidbCustomActionTypeVBScript         = 0x00000006;  // Target = entry point name; null if none to call
var msidbCustomActionTypeInstall          = 0x00000007;  // Target = property list for nested engine initialization

// source of code
var msidbCustomActionTypeBinaryData       = 0x00000000;  // Source = Binary.Name; data stored in stream
var msidbCustomActionTypeSourceFile       = 0x00000010;  // Source = File.File; file part of installation
var msidbCustomActionTypeDirectory        = 0x00000020;  // Source = Directory.Directory; folder containing existing file
var msidbCustomActionTypeProperty         = 0x00000030;  // Source = Property.Property; full path to executable

// return processing                  // default is syncronous execution; process return code
var msidbCustomActionTypeContinue         = 0x00000040;  // ignore action return status; continue running
var msidbCustomActionTypeAsync            = 0x00000080;  // run asynchronously
	
// execution scheduling flags               // default is execute whenever sequenced
var msidbCustomActionTypeFirstSequence    = 0x00000100;  // skip if UI sequence already run
var msidbCustomActionTypeOncePerProcess   = 0x00000200;  // skip if UI sequence already run in same process
var msidbCustomActionTypeClientRepeat     = 0x00000300;  // run on client only if UI already run on client
var msidbCustomActionTypeInScript         = 0x00000400;  // queue for execution within script
var msidbCustomActionTypeRollback         = 0x00000100;  // in conjunction with InScript: queue in Rollback script
var msidbCustomActionTypeCommit           = 0x00000200;  // in conjunction with InScript: run Commit ops from script on success
 
// security context flag; default to impersonate as user; valid only if InScript
var msidbCustomActionTypeNoImpersonate    = 0x00000800;  // no impersonation; run in system context
 
// We're provided with the path to the .msi we're building.
// We assume that this is in the same directory as all the other
// build outputs
var msiPath = WScript.Arguments(0);
var outDir = msiPath.substr(0, msiPath.lastIndexOf("\\") + 1);

var trayLauncherFile    = "SwerveToolsSetupTrayLauncher.dll";
var trayLauncherPath    = outDir + trayLauncherFile;
var appLauncherFile     = "SetupAppLauncher.exe";
var appLauncherPath     = outDir + appLauncherFile;
var swerveToolsTrayFile = "SwerveToolsTray.exe";

WScript.StdErr.WriteLine(WScript.ScriptName + ": processing '" + msiPath + "' with '" + appLauncherPath + "'");

try {
    //----------------------------------------------------------------------------------------------------------------------
    // Open the database
    //----------------------------------------------------------------------------------------------------------------------

    // Documentation on WindowsInstaller.Installer interface
    // https://msdn.microsoft.com/en-us/library/aa369432(v=vs.85).aspx
    var installer = WScript.CreateObject("WindowsInstaller.Installer");
    var database = installer.OpenDatabase(msiPath, msiOpenDatabaseModeTransact);
    var view;
    var record;
    var type;

    //----------------------------------------------------------------------------------------------------------------------
    // Disable both the modify and repair options in Add/Remove programs
    //----------------------------------------------------------------------------------------------------------------------

    // SQL syntax
    // https://msdn.microsoft.com/en-us/library/aa372021(v=vs.85).aspx
    // https://msdn.microsoft.com/en-us/library/aa368562(v=vs.85).aspx
    // https://github.com/Excel-DNA/WiXInstaller/blob/master/Source/WiRunSQL.vbs
    // https://msdn.microsoft.com/en-us/library/aa367523(v=vs.85).aspx              adding binary data

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
    // Change default selected button 'files in use' to 'continue' from Retry
    //----------------------------------------------------------------------------------------------------------------------

    view = database.OpenView("UPDATE `Dialog` SET `Dialog`.`Control_First`='ContinueButton'  WHERE `Dialog`.`Dialog`='FilesInUse'");
    view.Execute();
    view = database.OpenView("UPDATE `Dialog` SET `Dialog`.`Control_Default`='ContinueButton'  WHERE `Dialog`.`Dialog`='FilesInUse'");
    view.Execute();

    //----------------------------------------------------------------------------------------------------------------------
    // Add custom action to launch tray on setup exit
    // https://msdn.microsoft.com/en-us/library/aa367527(v=vs.85).aspx
    // https://msdn.microsoft.com/en-us/library/aa367521(v=vs.85).aspx
    //----------------------------------------------------------------------------------------------------------------------

    // Insert tray launcher dll as a binary blob in the MSI
    record = installer.CreateRecord(1);
    record.SetStream(1, trayLauncherPath);
    view = database.OpenView("INSERT INTO `Binary` (`Name`, `Data`) VALUES ('"+ trayLauncherFile +"', ?)");
    view.Execute(record);

    // Insert app launcher exe as a binary blob in the MSI
    record = installer.CreateRecord(1);
    record.SetStream(1, appLauncherPath);
    view = database.OpenView("INSERT INTO `Binary` (`Name`, `Data`) VALUES ('" + appLauncherFile + "', ?)");
    view.Execute(record);

    // Create a custom action that references tray launcher dll blob
    type = (msidbCustomActionTypeDll | msidbCustomActionTypeContinue);
    view = database.OpenView("INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('LaunchUsingDll', '" + type + "', '" + trayLauncherFile + "', 'LaunchTray')");
    view.Execute();

    // Create a custom action that references app launcher exe blob
    type = (msidbCustomActionTypeExe | msidbCustomActionTypeContinue);
    view = database.OpenView("INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('LaunchUsingExe', '" + type + "', '" + appLauncherFile + "', '[TARGETDIR]"+ swerveToolsTrayFile +"')");
    view.Execute();

    // Run custom action when the user closes the final dialog
    view = database.OpenView("INSERT INTO `ControlEvent` (`Dialog_`, `Control_`, `Event`, `Argument`, `Condition`, `Ordering`) VALUES ('FinishedForm', 'CloseButton', 'DoAction', 'LaunchUsingDll', 'NOT Installed', '1')");
    view.Execute();

    view = database.OpenView("INSERT INTO `ControlEvent` (`Dialog_`, `Control_`, `Event`, `Argument`, `Condition`, `Ordering`) VALUES ('FinishedForm', 'CloseButton', 'DoAction', 'LaunchUsingExe', 'NOT Installed', '2')");
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

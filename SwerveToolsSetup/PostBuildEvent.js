//
// PostBuildEvent.js
//

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

if (WScript.Arguments.Length !== 1)
    {
	WScript.StdErr.WriteLine(WScript.ScriptName + " file");
	WScript.Quit(1);
    }

var filespec = WScript.Arguments(0);

WScript.StdErr.WriteLine(WScript.ScriptName + ": processing '" + filespec + "'");

// Documentation on WindowsInstaller.Installer interface
// https://msdn.microsoft.com/en-us/library/aa369432(v=vs.85).aspx
var installer = WScript.CreateObject("WindowsInstaller.Installer");
var database = installer.OpenDatabase(filespec, msiOpenDatabaseModeTransact);

try {
    // SQL syntax
    // https://msdn.microsoft.com/en-us/library/aa372021(v=vs.85).aspx
    // https://msdn.microsoft.com/en-us/library/aa368562(v=vs.85).aspx
    // https://github.com/Excel-DNA/WiXInstaller/blob/master/Source/WiRunSQL.vbs
    
    // Disable both the modify and repair options in Add/Remove programs
    var view;
    view = database.OpenView("INSERT INTO `Property` (`Property`.`Property`, `Property`.`Value`) VALUES ('ARPNOMODIFY', '1')");
    view.Execute();

    view = database.OpenView("INSERT INTO `Property` (`Property`.`Property`, `Property`.`Value`) VALUES ('ARPNOREPAIR', '1')");
    view.Execute();

    view.Close();
	database.Commit();
    }
catch(e)
    {
	WScript.StdErr.WriteLine("exception thrown: " + e);
	WScript.Quit(1);
    }

WScript.StdOut.WriteLine(WScript.ScriptName + ": done");

//
// PostBuildEvent.js
//

// Constant values from Windows Installer
var msiOpenDatabaseModeTransact = 1;

var msiViewModifyInsert         = 1
var msiViewModifyUpdate         = 2
var msiViewModifyAssign         = 3
var msiViewModifyReplace        = 4
var msiViewModifyDelete         = 6

var msidbCustomActionTypeRollback       = 0x00000100;
var msidbCustomActionTypeCommit         = 0x00000200;
var msidbCustomActionTypeInScript       = 0x00000400;
var msidbCustomActionTypeNoImpersonate  = 0x00000800;

if (WScript.Arguments.Length != 1)
    {
	WScript.StdErr.WriteLine(WScript.ScriptName + " file");
	WScript.Quit(1);
    }

var filespec = WScript.Arguments(0);

WScript.StdErr.WriteLine(WScript.ScriptName + ": processing '" + filespec + "'");

var installer = WScript.CreateObject("WindowsInstaller.Installer");
var database = installer.OpenDatabase(filespec, msiOpenDatabaseModeTransact);

var sql
var view
var record
var action
var type
var source
var target
var custact
var noimp

try {
	sql = "SELECT `Action`, `Type`, `Source`, `Target` FROM `CustomAction`";
	view = database.OpenView(sql);
	view.Execute();
	record = view.Fetch();
    
	while (record) {
	    action = record.StringData(1);
        type = record.IntegerData(2);
        source = record.StringData(3);
        target = record.StringData(4);

        custact = (type & msidbCustomActionTypeInScript) ? "custact" : "normal";
        noimp = (type & msidbCustomActionTypeNoImpersonate) ? "noimp" : "imp";

        if ((type & msidbCustomActionTypeInScript) | 1) {
            WScript.StdOut.WriteLine(WScript.ScriptName + ": data: " + action + "|" + type.toString(16) + "|" + custact+"|" + noimp);
        }

        record = view.Fetch();
        }

	view.Close();
	database.Commit();
    }
catch(e)
    {
	WScript.StdErr.WriteLine(e);
	WScript.Quit(1);
    }

WScript.StdOut.WriteLine(WScript.ScriptName + ": done");

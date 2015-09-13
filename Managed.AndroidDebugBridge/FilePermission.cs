using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.ManagedADB {
	public class FilePermissions {

		public FilePermissions ( ) : this("---------") {
			
		}

		public FilePermissions (string permissions ) {
			if ( permissions.Length > 9 ) {
				permissions = permissions.Substring ( 1,9 );
			}

			if ( permissions.Length < 9 ) {
				throw new ArgumentException (string.Format("Invalid permissions string: {0}",permissions) );
			}

			User = new FilePermission ( permissions.Substring ( 0, 3 ) );
			Group = new FilePermission ( permissions.Substring ( 3, 3 ) );
			Other = new FilePermission ( permissions.Substring ( 6, 3 ) );
		}

		public FilePermissions ( FilePermission user, FilePermission group, FilePermission other ) {
			User = user;
			Group = group;
			Other = other;
		} 

		public FilePermission User { get; set; }
		public FilePermission Group { get; set; }
		public FilePermission Other { get; set; }


		public override string ToString ( ) {
			return string.Format ( "{0}{1}{2}", User.ToString ( ), Group.ToString ( ), Other.ToString ( ) );
		}

		public string ToChmod ( ) {
			return string.Format ( "{0}{1}{2}", (int)User.ToChmod ( ), (int)Group.ToChmod ( ), (int)Other.ToChmod ( ) );
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class FilePermission {

		/// <summary>
		/// 
		/// </summary>
		[Flags]
		public enum Modes {
			/// <summary>
			/// 
			/// </summary>
			NoAccess = 0,
			/// <summary>
			/// 
			/// </summary>
			Execute = 1,
			/// <summary>
			/// 
			/// </summary>
			Write = 2,
			/// <summary>
			/// 
			/// </summary>
			Read = 4
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Permission"/> class.
		/// </summary>
		public FilePermission ( )
			: this ( "---" ) {

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Permission"/> class.
		/// </summary>
		/// <param name="linuxPermissions">The linux permissions.</param>
		public FilePermission ( string linuxPermissions ) {
			this.CanRead = Util.equals ( linuxPermissions.Substring ( 0, 1 ), "r");
			this.CanWrite = Util.equals ( linuxPermissions.Substring ( 1, 1 ), "w");
			this.CanExecute = Util.equals ( linuxPermissions.Substring ( 2, 1 ), "x") || Util.equals ( linuxPermissions.Substring ( 2, 1 ), "t");
			this.CanDelete = this.CanWrite && !Util.equals ( linuxPermissions.Substring ( 2, 1 ), "t");
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance can execute.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can execute; otherwise, <c>false</c>.
		/// </value>
		public bool CanExecute { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance can write.
		/// </summary>
		/// <value><c>true</c> if this instance can write; otherwise, <c>false</c>.</value>
		public bool CanWrite { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance can read.
		/// </summary>
		/// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
		public bool CanRead { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance can delete.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can delete; otherwise, <c>false</c>.
		/// </value>
		public bool CanDelete { get; private set; }

		public override string ToString ( ) {
			StringBuilder perm = new StringBuilder ( );
			return perm.AppendFormat ( "{0}", CanRead ? "r" : "-" ).AppendFormat ( "{0}", CanWrite ? "w" : "-" ).AppendFormat ( "{0}", CanExecute ? CanDelete ? "x" : "t" : "-" ).ToString ( );
		}

		/// <summary>
		/// Converts the permissions to bit value that can be casted to an integer and used for calling chmod
		/// </summary>
		/// <returns></returns>
		public Modes ToChmod ( ) {
			Modes val = Modes.NoAccess;
			if ( CanRead ) {
				val |= Modes.Read;
			}

			if ( CanWrite ) {
				val |= Modes.Write;
			}

			if ( CanExecute ) {
				val |= Modes.Execute;
			}
			int ival = (int)val;
			return val;
		}

	}
}

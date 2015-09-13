using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.ManagedADB {
	/// <ignore>true</ignore>
	public static partial class ManagedAdbExtenstions {

		/// <summary>
		/// Combines the specified paths.
		/// </summary>
		/// <remarks>This wraps the normal System.IO.Path.Combine to allow for an unlimited list of paths to combine</remarks>
		/// <param name="paths">The paths.</param>
		/// <returns></returns>
		public static string Combine ( params string[] paths ) {
            string lastPath = string.Empty;

			foreach ( var item in paths ) {
				lastPath = Path.Combine ( lastPath, item );
			}

			return lastPath;
		}

	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.ManagedADB.IO {
	/// <summary>
	/// 
	/// </summary>
	/// <ignore>true</ignore>
	public class ByteOrder {

		private ByteOrder (string name ) {
			this.Name = name;
		}

		/// <summary>
		/// The big endian
		/// </summary>
		public readonly static ByteOrder BIG_ENDIAN = new ByteOrder ( "BIG_ENDIAN" );
		/// <summary>
		/// The little endian
		/// </summary>
		public readonly static ByteOrder LITTLE_ENDIAN = new ByteOrder ( "LITTLE_ENDIAN" );

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public string Name { get; private set; }
		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </returns>
		public override string ToString ( ) {
			return Name;
		}
	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Managed.Adb {
	internal sealed class LinkResoverReceiver : MultiLineReceiver {
		public LinkResoverReceiver ( ) {

		}

		public string ResolvedPath { get; set; }

		protected override void ProcessNewLines ( string[] lines ) {
			// all we care about is a line with '->'
			var regex = @"->\s+([^$]+)";
			foreach ( var line in lines.Where ( l => l.IsMatch(regex) ) ) {
				ResolvedPath = line.Match ( regex ).Groups[1].Value;
			}

		}
	}
}

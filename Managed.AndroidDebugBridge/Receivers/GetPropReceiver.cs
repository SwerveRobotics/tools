using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public sealed class GetPropReceiver : MultiLineReceiver
        {
        public  const string GETPROP_COMMAND = "getprop";
        private const string GETPROP_PATTERN = "^\\[([^]]+)\\]\\:\\s*\\[(.*)\\]$";

        public GetPropReceiver(Device device)
            {
            this.Device = device;
            }

        public Device Device
            {
            get; set;
            }

        protected override void ProcessNewLines(string[] lines)
            {
            // We receive an array of lines. We're expecting
            // to have the build info in the first line, and the build
            // date in the 2nd line. There seems to be an empty line
            // after all that.

            foreach (string line in lines)
                {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("$"))
                    {
                    continue;
                    }
                var m = line.Match(GETPROP_PATTERN, RegexOptions.Compiled);
                if (m.Success)
                    {
                    string label = m.Groups[1].Value.Trim();
                    string value = m.Groups[2].Value.Trim();

                    if (label.Length > 0)
                        {
                        Device.Properties.Add(label, value);
                        }
                    }
                }
            }

        protected override void Done()
            {
            this.Device.OnBuildInfoChanged(EventArgs.Empty);
            base.Done();
            }
        }
    }

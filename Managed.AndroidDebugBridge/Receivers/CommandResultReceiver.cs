using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public class CommandResultReceiver : MultiLineReceiver
        {
        protected override void ProcessNewLines(string[] lines)
            {
            StringBuilder result = new StringBuilder();
            foreach (string line in lines)
                {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("$"))
                    {
                    continue;
                    }

                result.AppendLine(line);
                }

            this.Result = result.ToString().Trim();
            }

        public string Result
            {
            get; private set;
            }
        }
    }

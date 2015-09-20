using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public sealed class EnvironmentVariablesReceiver : MultiLineReceiver
        {
        public const string ENV_COMMAND = "printenv";
        private const string ENV_PATTERN = @"^([^=\s]+)\s*=\s*(.*)$";

        public EnvironmentVariablesReceiver(Device device)
            {
            Device = device;
            }

        public Device Device
            {
            get; private set;
            }

        protected override void ProcessNewLines(string[] lines)
            {
            this.Device.EnvironmentVariables.Clear();

            foreach (string line in lines)
                {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    {
                    continue;
                    }

                Match m = Regex.Match(line, ENV_PATTERN);
                if (m.Success)
                    {
                    string label = m.Groups[1].Value.Trim();
                    string value = m.Groups[2].Value.Trim();

                    if (label.Length > 0)
                        {
                        if (Device.EnvironmentVariables.ContainsKey(label))
                            {
                            Device.EnvironmentVariables[label] = value;
                            }
                        else
                            {
                            Device.EnvironmentVariables.Add(label, value);
                            }
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

using System;
using System.Runtime.Serialization;

namespace Org.SwerveRobotics.Tools.ManagedADB.Exceptions
    {
    /** Signals a semantic error reported to us by the ADB server. */
    public class AdbException : Exception
        {
        public AdbException() : base("An error occurred with ADB")
            {
            }

        public AdbException(string message)
            : base(message)
            {
            }

        public AdbException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
            {
            }

        public AdbException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

    /** Exception thrown when adb refuses a command. */
    public class AdbCommandRejectedException : AdbException
        {
        public bool IsDeviceOffline
            {
            get; private set;
            }
        public bool WasErrorDuringDeviceSelection
            {
            get; private set;
            }

        public AdbCommandRejectedException(string message)
            : base(message)
            {
            IsDeviceOffline = message.Equals("device offline");
            WasErrorDuringDeviceSelection = false;
            }

        public AdbCommandRejectedException(string message, bool errorDuringDeviceSelection)
            : base(message)
            {
            WasErrorDuringDeviceSelection = errorDuringDeviceSelection;
            IsDeviceOffline = message.Equals("device offline");
            }
        }

    /** Unable to connect to the device because it was not found in the list of available devices. */
    public class DeviceNotFoundException : AdbException
        {
        public DeviceNotFoundException() : base("The device was not found.")
            {
            }
        public DeviceNotFoundException(string device)
            : base("The device '" + device + "' was not found.")
            {
            }
        }

    public class ShellCommandUnresponsiveException : AdbException
        {
        public ShellCommandUnresponsiveException() : base("The shell command has become unresponsive")
            {
            }
        }

    public class EndOfFileException : AdbException
        {
        }

    public class InvalidADBVersionException : AdbException
        {
        }

    public class ProcessErrorExitException : AdbException
        {
        public readonly int ExitCode;
        public ProcessErrorExitException(int exitCode)
            {
            this.ExitCode = exitCode;
            }
        }
    }
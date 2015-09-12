using System;
using System.Runtime.Serialization;

namespace Managed.Adb.Exceptions
    {
    public class UnknownOptionException : Exception
        {
        public UnknownOptionException()
            : base("Unknown option.")
            {
            }

        public UnknownOptionException(string message)
            : base(message)
            {
            }

        public UnknownOptionException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
            {
            }

        public UnknownOptionException(string message, Exception innerException)
            : base(message, innerException)
            {
            }
        }


    /** An exception while installing a package on the device. */
    [Serializable]
    public class PackageInstallationException : Exception
        {
        public PackageInstallationException()
            {
            }
        public PackageInstallationException(string message) : base(message)
            {
            }
        public PackageInstallationException(string message, Exception inner) : base(message, inner)
            {
            }
        protected PackageInstallationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
            {
            }
        }

    /** Thrown when an executed command identifies that it is being aborted. */
    public class CommandAbortingException : Exception
        {
        public CommandAbortingException()
            : base("Permission to access the specified resource was denied.")
            {
            }
        public CommandAbortingException(string message)
            : base(message)
            {
            }
        public CommandAbortingException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
            {
            }
        public CommandAbortingException(string message, Exception innerException)
            : base(message, innerException)
            {
            }
        }

    public class PermissionDeniedException : Exception
        {
        public PermissionDeniedException()
            : base("Permission to access the specified resource was denied.")
            {
            }
        public PermissionDeniedException(string message)
            : base(message)
            {
            }
        public PermissionDeniedException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
            {
            }
        public PermissionDeniedException(string message, Exception innerException)
            : base(message, innerException)
            {
            }
        }
    }

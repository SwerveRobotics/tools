using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Org.SwerveRobotics.Tools.Util
    {
    public static class Util
        {
        public static bool WaitOneNoExcept(this Mutex mutex, int msTimeout = -1)
            {
            try {
                return mutex.WaitOne(msTimeout);
                }
            catch (Exception)
                {
                return false;
                }
            }

        public static void Trace(string tag, string message)
            {
            System.Diagnostics.Trace.WriteLine($"{tag}: {message}");
            }

        //------------------------------------------------------------------------------
        // Naming mutexes etc
        //------------------------------------------------------------------------------
        
        public static string GlobalName(string name) => $"Global\\{name}";
        public static string UserName  (string name) => name;

        public static string GlobalName(string root, string uniquifier, string suffix)
            {
            return GlobalName($"SwerveTools{root}({uniquifier}){suffix}");
            }

        //---------------------------------------------------------------------------------------
        // ACL management
        //---------------------------------------------------------------------------------------

        public static SecurityIdentifier GetEveryoneSID()
            {
            return new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            }

        public static MutexSecurity MutexSecurity()
            {
            SecurityIdentifier user = GetEveryoneSID();
            MutexSecurity result = new MutexSecurity();

            MutexAccessRule rule = new MutexAccessRule(user, MutexRights.Synchronize | MutexRights.Modify | MutexRights.Delete, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }

        public static EventWaitHandleSecurity EventSecurity()
            {
            SecurityIdentifier user = GetEveryoneSID();
            EventWaitHandleSecurity result = new EventWaitHandleSecurity();

            EventWaitHandleAccessRule  rule = new EventWaitHandleAccessRule(user, EventWaitHandleRights.FullControl, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }

        public static MemoryMappedFileSecurity MapSecurity(bool create)
            {
            SecurityIdentifier user = GetEveryoneSID();
            MemoryMappedFileSecurity result = new MemoryMappedFileSecurity();

            MemoryMappedFileRights rights = MemoryMappedFileRights.ReadWrite;
            if (create)
                rights |= MemoryMappedFileRights.Delete;

            AccessRule<MemoryMappedFileRights> rule = new AccessRule<MemoryMappedFileRights>(user, rights, AccessControlType.Allow);
            result.AddAccessRule(rule);

            return result;
            }
        }
    }

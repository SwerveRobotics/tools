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
        //------------------------------------------------------------------------------
        // Threading and concurrency
        //------------------------------------------------------------------------------

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

        //------------------------------------------------------------------------------
        // Tracing and logging
        //------------------------------------------------------------------------------

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

        //---------------------------------------------------------------------------------------
        // Strings
        //---------------------------------------------------------------------------------------

        /// <summary>
        /// Interleave a character every so many characters of the source string
        /// </summary>
        public static string Interleave(this string target, char interleaved, int cInterleave)
            {
            StringBuilder result = new StringBuilder(target.Length * (cInterleave+1) / cInterleave);
            for (int i = 0; i < target.Length; i++)
                {
                if (i > 0 && i % cInterleave == 0) result.Append(interleaved);
                result.Append(target[i]);
                }
            return result.ToString();
            }

        /// <summary>
        /// Interleave a string between other strings in a list of strings
        /// </summary>
        public static string Interleave(this IEnumerable<string> target, string interleaved, int cInterleave=1)
            {
            int count = target.Count<string>();
            IEnumerator<string> enumerator = target.GetEnumerator(); 
            enumerator.MoveNext();
            //
            StringBuilder result = new StringBuilder(count * 10);   // wild ass guess
            for (int i = 0; i < count; i++)
                {
                if (i > 0 && i % cInterleave == 0) result.Append(interleaved);
                result.Append(enumerator.Current);
                enumerator.MoveNext();
                }
            return result.ToString();
            }

        public static string Select(this string str, Predicate<char> pred)
        // Return the subset of this string for which pred returns true.
            {
            StringBuilder result = new StringBuilder(str.Length);
            foreach (char ch in str)
                {
                if (pred(ch))
                    result.Append(ch);
                }
            return result.ToString();
            }

        public static string Select(this string str, string strAlphabet)
        // Return only those characters in str which are in the indicated alphabet
            {
            return str.Select(ch => strAlphabet.IndexOf(ch) != -1);
            }

        /// <summary>
        /// Returns a copy of the receiver but with the characters reversed
        /// </summary>
        public static string Reversed(this string str)
        // Extension method returning the string which is the reverse of the indicated one
            {
            char[] rgch = new char[str.Length];
            //
            for (int ich = 0; ich < str.Length; ich++)
                {
                rgch[ich] = str[str.Length -1 -ich];
                }
            //
            return new string(rgch);
            }

        /// <summary>
        /// Returns a copy of the receiver with each of its characters mapped through the provided map
        /// </summary>
        public static string TranslateBy(this string str, IDictionary<char, char> map)
            {
            StringBuilder result = new StringBuilder(str.Length);
            foreach (char chFrom in str)
                {
                char chTo;
                if (!map.TryGetValue(chFrom, out chTo))
                    chTo = chFrom;
                //
                result.Append(chTo);
                }
            return result.ToString();
            }

        /// <summary>
        /// Returns a copy of the receiver with each of its characters mapped through the provided map
        /// </summary>
        public static string TranslateBy(this string str, Func<char,char> map)
            {
            StringBuilder result = new StringBuilder(str.Length);
            foreach (char chFrom in str)
                {
                char chTo = map(chFrom);
                result.Append(chTo);
                }
            return result.ToString();
            }

        public static string SafeSubstringLast(this string str, int ichLast, int cch)
            {
            return SafeSubstring(str, str.Length-cch-ichLast , cch);
            }

        public static string SafeSubstring(this string str, int ichFirst)
            {
            return SafeSubstring(str, ichFirst, str?.Length ?? 0);
            }

        public static string SafeSubstring(this string str, int ichFirst, int cch)
            {
            if (ichFirst < 0)
                {
                cch += ichFirst;
                ichFirst = 0;
                }
            if (str != null && str.Length > ichFirst)
                {
                cch = Math.Min(cch, str.Length - ichFirst);
                if (cch > 0)
                    {
                    return str.Substring(ichFirst, cch);
                    }
                }
            return "";
            }

        public static bool IsPrefixOf(this string prefix, string target)
            {
            if (string.IsNullOrEmpty(prefix))
                return true;
            else
                return prefix == target.SafeSubstring(0, prefix.Length);
            }
        }
    }

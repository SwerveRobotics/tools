//
// Util.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.SwerveRobotics.BotBug.Service
    {
    //------------------------------------------------------------------------------------------------
    // Util
    //------------------------------------------------------------------------------------------------

    public static class Util
        {
        //--------------------------------------------------------------------------------------------
        // String
        //--------------------------------------------------------------------------------------------

        public unsafe static String ToStringUni(void* psz, long cbMax = Int64.MaxValue)
            { 
            long cchMax = cbMax / sizeof(char);
            char *pch = (char*) psz;
            StringBuilder result = new StringBuilder();
            while (*pch != '\0' && result.Length < cchMax)
                {
                result.Append(*pch++);
                }
            return result.ToString();
            }

        public unsafe static String ToStringAnsi(void* psz, long cbMax = Int64.MaxValue)
            { 
            long cchMax = cbMax;
            byte *pch = (byte*) psz;
            StringBuilder result = new StringBuilder();
            while (*pch != '\0' && result.Length < cchMax)
                {
                result.Append(*pch++);
                }
            return result.ToString();
            }

        static Dictionary<string, Regex> existingRegex = new Dictionary<string,Regex>();

        public static Regex ConstructRegex(string expression)
            {
            Regex result = null;
            //
            if (expression.Length > 0)
                {
                if (!existingRegex.TryGetValue(expression, out result))
                    {    
                    // Actually construct a new recognizer
                    //
                    RegexOptions options = 
                          RegexOptions.IgnoreCase 
                        | RegexOptions.Compiled
                        | RegexOptions.CultureInvariant
                        | RegexOptions.Singleline
                        ;
                    //
                    result = new Regex(expression, options);
                    //
                    // Remember it for later
                    //
                    existingRegex[expression] = result;
                    }
                }
            return result;
            }

        public static bool IsIdentifier(this string self)
        // Is this string a Nadir identifier?
            {
            Regex regex = ConstructRegex(@"^([a-z]|[A-Z]|_)([a-z]|[A-Z]|[0-9]|_)*$");   // see Nadir.g for reference
            return regex.IsMatch(self);
            }

        public static int CompareInvariantTo(this string self, string him)
            {
            return System.Globalization.CultureInfo.InvariantCulture.CompareInfo.Compare(self, him, System.Globalization.CompareOptions.None);
            }

        public static bool IsPalindromic(this string str)
            {
            return str.Reversed() == str;
            }

        public static string Chuncked(this string str, int cchChunk=3)
            {
            return str.Interleave(' ', cchChunk);
            }

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
            return SafeSubstring(str, ichFirst, str != null ? str.Length : 0);
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

        static public string WithQuotes(this string s)
            {
            StringBuilder result = new StringBuilder(s.Length+2);
            result.Append('"');
            foreach (char ch in s)
                {
                if ('"'==ch)
                    result.Append("\\\"");
                else
                    result.Append(ch);
                }
            result.Append('"');
            return result.ToString();
            }


        /// <summary>
        /// Answer whether this character is a legal hexadecimal digit.
        /// </summary>
        static bool IsHexDigit(this char ch)
            {
            switch (ch)
                {
            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9': 
            case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
            case 'A': case 'B': case 'C': case 'D': case 'E': case 'F':
                return true;
            default: return false;
                }
            }

        /// <summary>
        /// Return the hexadecimal value of this character
        /// </summary>
        static int HexValue(this char ch)
            {
            switch (ch)
                {
            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9': 
                return (int)ch - (int)'0';
            case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
                return (int)ch - (int)'a';
            case 'A': case 'B': case 'C': case 'D': case 'E': case 'F':
                return (int)ch - (int)'A';
            default: return 0;              // illegal char: ignore
                }
            }

        /// <summary>
        /// Process Nadir escape sequences in the string, replacing them with the characters represented
        /// </summary>
        public static string DecodeNadirEscapeCharacters(this string str, bool quotesOnly=false)
        // Process escape sequences in this string. REVIEW: we have the Nadir ones here, which are 
        // similar to but maybe not identical to the C# ones. Should we generalize?
            {
            StringBuilder result = new StringBuilder(str.Length);
            for (int ich = 0, cch = str.Length; ich < cch; ich++)
                {
                char ch = str[ich];
                switch (ch)
                    {
                case '\\':
                    ich++;
                    if (ich < cch)
                        {
                        ch = str[ich];
                        if (quotesOnly)
                            {
                            if (ch == '\"')
                                {
                                }
                            else
                                {
                                result.Append('\\');
                                }
                            }
                        else
                            {
                            switch (ch)
                                {
                            case 'b':   ch = '\b'; break;
                            case 't':   ch = '\t'; break;
                            case 'n':   ch = '\n'; break;
                            case 'f':   ch = '\f'; break;
                            case 'r':   ch = '\r'; break;
                            case '\"':  ch = '\"'; break;
                            case '\'':  ch = '\''; break;
                            case '\\':  ch = '\\'; break;
                            case '0':
                                // octal: one to three octal digits
                                int oval = 0;
                                ich++; if (ich < cch) { ch=str[ich]; if ('0'<=ch && ch<='3') { oval = oval*8 + (int)ch-(int)'0';
                                ich++; if (ich < cch) { ch=str[ich]; if ('0'<=ch && ch<='7') { oval = oval*8 + (int)ch-(int)'0';
                                ich++; if (ich < cch) { ch=str[ich]; if ('0'<=ch && ch<='7') { oval = oval*8 + (int)ch-(int)'0';
                                } else ich--; } } else ich--; } } else ich--; }
                                ch = (char)oval;
                                break;
                            case 'u':
                                // unicode: four hex digits
                                int uval = 0;
                                ich++; if (ich < cch) { uval = uval*16 + HexValue(str[ich]); }
                                ich++; if (ich < cch) { uval = uval*16 + HexValue(str[ich]); }
                                ich++; if (ich < cch) { uval = uval*16 + HexValue(str[ich]); }
                                ich++; if (ich < cch) { uval = uval*16 + HexValue(str[ich]); }
                                ch = (char)uval;
                                break;
                            default:
                                // technically illegal: we just escape '\' ch to itself
                                result.Append('\\');
                                break;
                                }
                            }
                        }
                    break;
                default:
                    break;
                    }
                result.Append(ch);
                }
            string strResult = result.ToString();
            return strResult;
            }
    
        public static void AppendLine(this StringBuilder build, string format, params object[] args)
            {
            build.AppendLine(string.Format(format, args));
            }
        public static void Append(this StringBuilder build, string format, params object[] args)
            {
            build.Append(string.Format(format, args));
            }

        public static string Replicate(this string replicon, int count)
            {
            if (null == replicon) return null;
            StringBuilder result = new StringBuilder(replicon.Length * count);
            for (int i = 0; i < count; i++)
                {
                result.Append(replicon);
                }
            return result.ToString();
            }
        public static string Replicate(this char replicon, int count)
            {
            StringBuilder result = new StringBuilder(1 * count);
            for (int i = 0; i < count; i++)
                {
                result.Append(replicon);
                }
            return result.ToString();
            }

        //--------------------------------------------------------------------------------------------
        // Errors and tracing
        //--------------------------------------------------------------------------------------------

        public static void TraceStdOut(string tag, string sFormat, params object[] data)
            {
            String payload = String.Format(sFormat, data);
            System.Console.WriteLine("{0}: {1}", tag, payload);
            }

        public static void TraceDebug(string tag, string sFormat, params object[] data)
            {
            String payload = String.Format(sFormat, data);
            System.Diagnostics.Debug.WriteLine("{0}: {1}", tag, payload);
            }

        public static void TraceDebug(string sFormat, params object[] data)
            {
            System.Diagnostics.Debug.WriteLine(sFormat, data);
            }

        public static void ReportError(string sFormat, params object[] data)
            {
            TraceDebug(sFormat, data);
            }

        public static RET_T Fail<RET_T>()
            {
            System.Diagnostics.Debug.Fail("program exiting");
            Environment.Exit(-1);
            return default(RET_T);
            }

        //--------------------------------------------------------------------------------------------
        // Misc
        //--------------------------------------------------------------------------------------------

        public static string IpAddress(this Managed.Adb.IDevice device)
            {
            return device.GetProperty("dhcp.wlan0.ipaddress");
            }

        public static string[] Lines(this string str, StringSplitOptions options = StringSplitOptions.None)
        // Split the string into its various lines, being careful about the various forms of 
        // line endings that exist in the world.
            {
            string[] delims = new string[] { "\r\n", "\r", "\n" };
            return str.Split(delims, options);
            }

        public static byte[] Slice(this byte[] rgb, int ibFirst, int cb)
            {
            if (rgb != null)
                {
                cb = Math.Min(cb, rgb.Length - ibFirst);
                byte[] result = new byte[cb];
                Array.ConstrainedCopy(rgb, ibFirst, result, 0, cb);
                return result;
                }
            else
                return new byte[0];
            }
        public static byte[] Slice(this byte[] rgb, int ibFirst)
            {
            return Slice(rgb, ibFirst, rgb != null ? rgb.Length : 0);
            }

        public static bool IsEqualTo<TSource>(this IEnumerable<TSource> value, IEnumerable<TSource> compareList, IEqualityComparer<TSource> comparer)
        // Compare two collections for equality of members
            {
            if (value == compareList)
                {
                return true;
                }
            else if (value == null || compareList == null)
                {
                return false;
                }
            else
                {
                if (comparer == null)
                    {
                    comparer = EqualityComparer<TSource>.Default;
                    }

                IEnumerator<TSource> enumerator1 = value.GetEnumerator();
                IEnumerator<TSource> enumerator2 = compareList.GetEnumerator();

                bool enum1HasValue = enumerator1.MoveNext();
                bool enum2HasValue = enumerator2.MoveNext();

                try
                    {
                    while (enum1HasValue && enum2HasValue)
                        {
                        if (!comparer.Equals(enumerator1.Current, enumerator2.Current))
                            {
                            return false;
                            }

                        enum1HasValue = enumerator1.MoveNext();
                        enum2HasValue = enumerator2.MoveNext();
                        }

                    return !(enum1HasValue || enum2HasValue);
                    }
                finally
                    {
                    if (enumerator1 != null) enumerator1.Dispose();
                    if (enumerator2 != null) enumerator2.Dispose();
                    }
                }
            }

        public static bool IsEqualTo<TSource>(this IEnumerable<TSource> value, IEnumerable<TSource> compareList)
            {
            return IsEqualTo(value, compareList, null);
            }

        public static bool IsEqualTo(this System.Collections.IEnumerable value, System.Collections.IEnumerable compareList)
            {
            return IsEqualTo<object>(value.OfType<object>(), compareList.OfType<object>());
            }
        }
    }

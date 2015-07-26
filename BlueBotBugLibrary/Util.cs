//
// Util.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.Library
    {
    //------------------------------------------------------------------------------------------------
    // Util
    //------------------------------------------------------------------------------------------------

    static class Util
        {
        public static void Trace(string sFormat, params object[] data)
            {
            System.Diagnostics.Debug.WriteLine(sFormat, data);
            }

        public static void ReportError(string sFormat, params object[] data)
            {
            Trace(sFormat, data);
            }

        public static RET_T Fail<RET_T>()
            {
            System.Diagnostics.Debug.Fail("program exiting");
            Environment.Exit(-1);
            return default(RET_T);
            }

        public static bool IsPrefixOfIgnoreCase(this String sPrefix, string sTarget)
            {
            if (sTarget.Length >= sPrefix.Length)
                {
                return String.Compare(sTarget.Substring(0, sPrefix.Length), sPrefix, true) == 0;
                }
            return false;
            }

        public static string SafeSubstring(this string str, int ichFirst)
            {
            return SafeSubstring(str, ichFirst, str != null ? str.Length : 0);
            }
        public static string SafeSubstring(this string str, int ichFirst, int cch)
            {
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

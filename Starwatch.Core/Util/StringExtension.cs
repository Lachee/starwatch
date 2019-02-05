using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Util
{
    public static class StringExtension
    {
        /// <summary>
        /// Cuts a segment of the string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start">The starting index</param>
        /// <param name="end">The ending index</param>
        /// <returns></returns>
        public static string Cut(this string str, int start, int end)
        {
            if (end > str.Length) throw new ArgumentOutOfRangeException("end", "The end cannot be greater than the length of the string");
            if (start > end) throw new ArgumentOutOfRangeException("start", "The start cannot be greater than the end");
            return str.Substring(start, end - start);
        }
    }
}

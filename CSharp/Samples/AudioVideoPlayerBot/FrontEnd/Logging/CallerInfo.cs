/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

namespace FrontEnd.Logging
{
    /// <summary>
    /// Class that encapsulates the caller's (creator's) information.  This is helpful to provide more context in log statements.
    /// </summary>
    public class CallerInfo
    {
        private static ConcurrentDictionary<int, string> toStringCache = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// The name of the method or property of the caller
        /// </summary>
        public string MemberName { get; protected set; }

        /// <summary>
        /// The full path of the source file of the caller
        /// </summary>
        public string FilePath { get; protected set; }

        /// <summary>
        /// The line number of the source file of the caller
        /// </summary>
        public int LineNumber { get; protected set; }

        /// <summary>
        /// Creates a new instance of the CallerInfo class
        /// </summary>
        public CallerInfo(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
            )
        {
            this.MemberName = memberName;
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
        }

        /// <summary>
        /// Get the hashcode for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return MemberName.GetHashCode() ^ FilePath.GetHashCode() ^ LineNumber;
        }

        /// <summary>
        /// String representation of the caller's info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return toStringCache.GetOrAdd(this.GetHashCode(), hc => String.Format(
                "{0},{1}({2})",
                this.MemberName,
                Path.GetFileName(this.FilePath),
                this.LineNumber
                ));
        }
    }
}

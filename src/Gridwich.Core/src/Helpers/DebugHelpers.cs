using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Miscellaneous static helper functions to assist with Debugging.
    /// </summary>
    public static class DebugHelpers
    {
        // If true, include an extra message prefix including the
        // managed thread ID on each Write*.  e.g. "[22]"
        //
        // Note, should really be const, but readonly/static avoids
        // dead code warnings in GetThreadPrefix.
        // Disabling: warning CA1802: Field 'IncludeThreadId' is declared as 'readonly' but is initialized with a constant value. Mark this field as 'const' instead.
        // warning CA1802:
        [field: SuppressMessage("Microsoft.Performance", "CA1802:UseLiteralsWhereAppropriate",
            Justification = "As above, readonly vs. const to be able to leave debugging code in an sidestep dead code warnings")]
        private static readonly bool IncludeThreadId = true;

        /// <summary>
        /// Output to debug listeners.  Used to get around limitations in Xunit where
        /// it does not include any Write/WriteLine output in the actual output (due
        /// to some limitation in the Visual Studio test runner).
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public static void WriteLine(string format, params object[] args)
        {
            string s;

            // If there aren't any arguments, avoid the possibility of
            // Format interpreting slashes, or substitutions in the
            // format operand.  i.e., if no args, just directly dump
            // the string with interpretaions.
            if (args == null || args.Length == 0)
            {
                s = format;
            }
            else
            {
                s = StringHelpers.Format(format, args);
            }

            Debug.WriteLine(GetThreadPrefix() + s);
            Debug.Flush();
        }

        private static string GetThreadPrefix()
        {
            if (!IncludeThreadId)
            {
                return string.Empty;
            }
            else
            {
                return $"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] ";
            }
        }

        /// <summary>
        /// Get File/Line/Column coordinate information string for the specified stack frame.
        /// </summary>
        /// <param name="offsetFromHere">
        /// A non-negative number indicating how far back up the StackTrace to climb to
        /// locate the frame of interest. 0 means the function calling GetStackCoordinates,
        /// 1 means the caller of that function, etc.
        /// </param>
        /// <param name="startOfInterest">
        /// A string indicating the start of the file path portion which should be retained.
        /// For example: given "/xyz", the file "C:\the\path\xyz\myFile.c" would become
        /// "xyz/myFile.C".  Note the use of forward slashes and that the final value does
        /// not start with a slash.
        /// This has a default value of "Gridwich." to simplify use within this project.
        /// </param>
        /// <returns>
        /// A string formatted as "fileName:lineNo.columnNo" if the information is
        /// available. Otherwise string.Empty. This latter case is usually true with
        /// non-Debug builds.
        /// </returns>
        public static string GetStackCoordinates(int offsetFromHere, string startOfInterest = "Gridwich.")
        {
            // +1 is to bypass this function's stack frame.
            StackFrame sf = new StackFrame(offsetFromHere + 1, true);
            var fileName = sf.GetFileName();

            if (fileName == null)
            {
                // No coordinate information available :-(
                // Likely because this is a non-debug build.
                return string.Empty;
            }

            fileName = fileName.Replace('\\', '/');
            int idx = fileName.IndexOf(startOfInterest, StringComparison.InvariantCultureIgnoreCase);
            if (idx > -1)
            {
                // found it, so drop before that point
                fileName = fileName.Substring(idx);
            }
            return $"{fileName}:{sf.GetFileLineNumber()}.{sf.GetFileColumnNumber()}";
        }
    }
}
using System;
using System.Globalization;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Miscellaneous static helper functions to assist with Debugging.
    /// </summary>
    public static class StringHelpers
    {
        /// <summary>
        /// Like String.Format, except enforcing the invariant culture.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        ///   <see cref="String"/>
        /// </returns>
        public static string Format(string format, params object[] args)
        {
            _ = format ?? throw new System.ArgumentNullException(nameof(format));

            string s = string.Format(CultureInfo.InvariantCulture, format, args);
            return s;
        }

        /// <summary>
        /// Camelizes the specified name.
        /// </summary>
        /// <param name="name">The name to be camel-ized (swap first letter to lower case).</param>
        /// <returns>Camel-ized name</returns>
        public static string Camelize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            // otherwise, lower-case the first letter & return
            var firstChar = char.ToLower(name[0], CultureInfo.InvariantCulture);
            string remainder = string.Empty;

            if (name.Length > 1)
            {
                remainder = name.Substring(1);
            }

            var ret = $"{firstChar}{remainder}";

            return ret;
        }

        /// <summary>
        /// Return null if the input string is null, or empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>
        /// Null if the string is null or empty.  Otherwise the input string.
        /// </returns>
        /// <remarks>
        /// The primary use is for null-checking at the start of methods.  This
        /// allows for using null-coalesing with a more involved check, and throwing
        /// an exception, all in a single line, decreasing vertical code bulk, e.g.
        /// <example>
        ///    _ = StringHelpers.NullIfNullOrEmpty(param1) ?? throw new ArgumentException(nameof(param1));
        /// </example>
        /// versus
        /// <example>
        ///    if (string.IsNullOrEmpty(param1))
        ///    {
        ///        throw new ArgumentException(nameof(param1));
        ///    }
        /// </example></remarks>
        public static string NullIfNullOrEmpty([ValidatedNotNull] string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            return s;
        }

        /// <summary>
        /// Return null if the input string is null, or all whitespace (i.e., string.IsNullOrWhitespace())
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>
        /// Null if the string is null or whitespace.  Otherwise the input string.
        /// </returns>
        /// <remarks>
        /// The primary use is for null-checking at the start of methods.  This
        /// allows for using null-coalesing with a more involved check, and throwing
        /// an exception, all in a single line, decreasing vertical code bulk, e.g.
        /// <example>
        ///    _ = StringHelpers.NullIfNullOrWhitespace(param1) ?? throw new ArgumentException(nameof(param1));
        /// </example>
        /// versus
        /// <example>
        ///    if (string.NullIfNullOrWhitespace(param1))
        ///    {
        ///        throw new ArgumentException(nameof(param1));
        ///    }
        /// </example></remarks>
        public static string NullIfNullOrWhiteSpace([ValidatedNotNull] string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }
            return s;
        }

        /// <summary>
        /// The usual string.ToLowerInvariant(), but without the code analysis
        /// complaint about how it's better to force to upper-case before comparing.
        /// </summary>
        /// <param name="s">string</param>
        /// <returns>Lowercased string</returns>
        [method: System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase",
            Justification = "Strictly checking if all lowercase, so exactly as desired")]
        public static string ToLowerInvariant(string s)
        {
            _ = s ?? throw new ArgumentNullException(nameof(s));

            return s.ToLowerInvariant();
        }
    }
}
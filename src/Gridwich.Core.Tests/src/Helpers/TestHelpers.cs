using Gridwich.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Just what it sounds like -- things to make writing unit tests a bit easier
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Create simple guid-based storage contexts for Test cases.
        /// </summary>
        /// <param name="guidToUse">The unique identifier to use.</param>
        /// <param name="trackETag">if set to <c>true</c> [track e tag].</param>
        /// <param name="eTagValue">The e tag value.</param>
        /// <returns>
        ///   <see cref="StorageClientProviderContext"/>
        /// </returns>
        public static StorageClientProviderContext CreateGUIDContext(Guid? guidToUse = null, bool trackETag = false, string eTagValue = null)
        {
            Guid g = (guidToUse == null && guidToUse.HasValue) ? guidToUse.Value : Guid.NewGuid();

            var opContext = $"{{\"guid\":\"{g}\"}}";  // some simple JSON to house the GUID
            var ret = new StorageClientProviderContext(
                opContext, muted: false, trackETag: trackETag, initialETag: eTagValue);
            return ret;
        }

        /// <summary>
        /// Recalculate a file path, relative to the named level in the current directory path.  Note that this file does
        /// not perform existence/validity checks for any of the calculated path components or file.  Path elements of ".."
        /// and "." are taken into account and processed out of the final result.
        /// </summary>
        /// <param name="startDirectoryComponent">The level in the current directory path at which the new path should originate.
        /// e.g. "src" with a current directory of "C:\u\fred\xyz\src\jim\stuff" would use "C:\u\fred\xyz\src\" as the start of the
        /// path being calculated.</param>
        /// <param name="filePathRelativeToStart">
        /// The suffix of the desired file path, relative to starting point specified in <paramref name="startDirectoryComponent" />.
        /// e.g. "info\jim\..\static.json" could give "c:\u\fred\syz\src\info\static.json".</param>
        /// <param name="currentDirectory">currentDirectory.</param>
        /// <param name="desiredSeparator">
        /// The desired output path component separator.  The default is '/', which will work both with Linux and
        /// Win32 File APIs.  Note that for input, the '\' and '/' separators are accepted as equivalent, with the output being
        /// mormalized to use the <paramref name="desiredSeparator" /> value.</param>
        /// <returns>
        /// The calculated file or directory path with components separated by the <paramref name="desiredSeparator" /> value.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown if the named component (<paramref name="startDirectoryComponent" />)
        /// does not occur the current directory path; preventing being able to form a complete result path, or if
        /// <paramref name="filePathRelativeToStart" /> contains '..' components which exceed the depth of the
        /// current directory</exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if either argument (<paramref name="startDirectoryComponent" /> or <paramref name="filePathRelativeToStart" />) is
        /// "empty" or whitespace, or null.</exception>
        public static string GetPathRelativeTo(string startDirectoryComponent, string filePathRelativeToStart, string currentDirectory = null, char desiredSeparator = '/')
        {
            if (string.IsNullOrWhiteSpace(startDirectoryComponent))
            {
                throw new ArgumentException($"Empty or null {nameof(startDirectoryComponent)} argument", nameof(startDirectoryComponent));
            }
            if (string.IsNullOrWhiteSpace(filePathRelativeToStart))
            {
                throw new ArgumentException($"Empty or null {nameof(filePathRelativeToStart)} argument", nameof(filePathRelativeToStart));
            }

            string cd = currentDirectory ?? Directory.GetCurrentDirectory();

            // internally, just work all in forward slashes for simplicity.

            // Normalize the naming - specifically separators
            cd = cd.Replace('\\', '/');
            // options is to handle case where directory path does & doesn't have a trailing slash
            var cdParts = cd.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // remember if path started with a slash
            bool hadLeadingSlash = cd.StartsWith('/');

            string pathPrefix = cdParts[0];
            bool foundStart = false;

            for (int i = cdParts.Length - 1; i >= 0; i--)
            {
                // noted: case sensitive is intentional so that it works on Linux & Windows
                if (cdParts[i] == startDirectoryComponent)
                {
                    if (i > 0)
                    {
                        pathPrefix = string.Join(desiredSeparator, cdParts, 0, i + 1);
                        foundStart = true;
                        break;
                    }
                }
            }

            if (!foundStart)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startDirectoryComponent),
                    $"Start level '{startDirectoryComponent}' not found in current directory path: '{cd}'");
            }

            // Now have the starting point in current tree, now massage together the complete path.

            filePathRelativeToStart = filePathRelativeToStart.Replace('\\', '/');

            if (filePathRelativeToStart.StartsWith('/'))
            {
                // simplest to "/" => "./"
                filePathRelativeToStart = $".{filePathRelativeToStart}";
            }

            var fullPath = $"{pathPrefix}/{filePathRelativeToStart}";

            // Now normalize out .. and .

            fullPath = fullPath.Replace("/./", "/");

            // extra processing needed for climbing the directory tree?
            if (fullPath.Contains("/../"))
            {
                // simplest is to walk across the list and process any '..' as we walk.
                var fullParts = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                int j = 0; // point to next slot to fill
                for (int i = 0; i < fullParts.Length; i++)
                {
                    if (fullParts[i] != "..")
                    {
                        fullParts[j] = fullParts[i];
                        j++;
                    }
                    else
                    {
                        j--;   // decrement j, effectively erasing the previous component
                        if (j < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(startDirectoryComponent),
                                $"Excess '..' components when processing '{startDirectoryComponent}' against current directory path: '{cd}'");
                        }
                    }
                }
                fullPath = string.Join('/', fullParts, 0, j);
            }

            if (hadLeadingSlash && !fullPath.StartsWith('/'))
            {
                fullPath = $"/{fullPath}"; // put back the slash
            }

            // re-normalize the separator, if needed.
            if (desiredSeparator != '/')
            {
                fullPath = fullPath.Replace('/', desiredSeparator);
            }

            return fullPath;
        }

        /// <summary>
        /// Recalculate a file path, relative to the build tree's src directory from a current directory below that.
        /// Note that this file does not perform existence/validity checks for any of the calculated path components
        /// or file.  Path elements of ".." and "." are taken into account and processed out of the final result.
        ///
        /// See <see cref="GetPathRelativeTo"/> for more information re exceptions and parameters.
        /// </summary>
        /// <param name="filePathRelativeToTests">The filePathRelativeToSrc.</param>
        /// <param name="separator">The separator.</param>
        public static string GetPathRelativeToSrc(string filePathRelativeToSrc, char separator = '/')
        {
            return GetPathRelativeTo("src", filePathRelativeToSrc, null, separator);
        }

        /// <summary>
        /// Recalculate a file path, relative to the build tree's tests directory from a current directory below that.
        /// Note that this file does not perform existence/validity checks for any of the calculated path components
        /// or file.  Path elements of ".." and "." are taken into account and processed out of the final result.
        ///
        /// See <see cref="GetPathRelativeTo"/> for more information re exceptions and parameters.
        /// </summary>
        /// <param name="filePathRelativeToTests">The filePath relative to the tests subdirectory.</param>
        /// <param name="separator">The separator.</param>
        public static string GetPathRelativeToTests(string filePathRelativeToTests, char separator = '/')
        {
            return GetPathRelativeTo("tests", filePathRelativeToTests, null, separator);
        }

        /// <summary>
        /// Return a dictionary of the enumerator names and values for the specified enumeration type.
        /// </summary>
        /// <typeparam name="TEnumType">The enumeration type</typeparam>
        /// <typeparam name="TEnumBase">The base type for the enumeration.  Usually just <c>int<c>.</typeparam>
        /// <returns>
        /// An <c>IDictionary&lt;string, TEnumBase&gt;</c> where the keys are the enumerator names and the
        /// values are the corresponding value of an enumerator.
        /// </returns>
        public static IDictionary<string, TEnumBase> GetEnumerators<TEnumType, TEnumBase>()
            where TEnumType : System.Enum
        {
            var res = new Dictionary<string, TEnumBase>(20);

            var enumeratorNames = Enum.GetNames(typeof(TEnumType));
            // The values will be in the same order as the names
            var enumValuesRaw = Enum.GetValues(typeof(TEnumType));

            for (int i = 0; i < enumeratorNames.Length; i++)
            {
                var val = (TEnumBase)enumValuesRaw.GetValue(i);
                res[enumeratorNames[i]] = val;
            }

            return res;
        }
    }
}

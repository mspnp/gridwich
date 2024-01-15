using Gridwich.Core.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Gridwich.Core.Bases
{
    /// <summary>
    /// Base implementation of <c>IAppInsightsUrlCreator</c>
    /// </summary>
    /// <seealso cref="Gridwich.Core.Interfaces.IAppInsightsUrlCreator" />
    public abstract class AppInsightsUrlCreatorBase : IAppInsightsUrlCreator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppInsightsUrlCreatorBase"/> class.
        /// </summary>
        protected AppInsightsUrlCreatorBase() { }

        /// <inheritdoc/>
        public abstract Uri CreateUrl(string query);

        /// <summary>
        /// Compresses an application insights query.
        /// </summary>
        /// <param name="query">The query to compress</param>
        /// <returns>A gzip-compressed, base64-encoded version of the query</returns>
        protected static string CompressAppInsightsQuery(string query)
        {
            // Step 1: gzip compress the query
            byte[] gzippedStringBytes;
            using (var gzipHolder = new MemoryStream())
            {
                using (var gzipper = new GZipStream(gzipHolder, CompressionMode.Compress))
                {
                    gzipper.Write(Encoding.UTF8.GetBytes(query));
                }

                gzippedStringBytes = gzipHolder.ToArray();
            }

            // Step 2: base64-encode the compressed string
            var b64string = Convert.ToBase64String(gzippedStringBytes);

            // Step 3: URL encode the base64-encoded value (since it IS going in a URL after all)
            var urlEncodedB64string = System.Web.HttpUtility.UrlEncode(b64string);
            return urlEncodedB64string;
        }
    }
}

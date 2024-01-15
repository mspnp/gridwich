using Azure.Storage.Sas;
using Gridwich.Core.Constants;
using System;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Helpers related to the Storage Service
    /// </summary>
    public static class StorageHelpers
    {
        /// <summary>
        /// Generate a TimeRange instance covering UTC time from:
        /// Now - SecondToBackUp (account for possible clock skew) to Now + ttl
        /// </summary>
        public static TimeRange CreateTimeRangeForUrl(TimeSpan ttl)
        {
            var ret = new TimeRange(StorageServiceConstants.SecondsToBackDateForSasUrl, DateTimeOffset.UtcNow, ttl);
            return ret;
        }

        /// <summary>
        /// Construct a URI for a Storage Container or Blob.
        /// e.g. https://myacct.blob.core.windows.net/myContainer/Setup.TXT
        /// </summary>
        /// <param name="accountName">The unqualified name of storage account (e.g., "myacct").
        /// No upper-case letters permited.</param>
        /// <param name="containerName">The unqualified storage container name (e.g., "myContainer")</param>
        /// <param name="blobName">The optional, unqualified name of the blob (e.g., "Setup.TXT").
        /// The blobName is optional.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if any of the arguments are null or either of
        /// accountName or containerName are strings only containing whitespace</exception>
        /// <exception cref="System.ArgumentException">Thrown if accountName contains any upper-case letters or
        /// special characters.  e.g., if "agcD" rather than "agcd"</exception>
        public static Uri BuildBlobStorageUri(string accountName, string containerName, string blobName = "")
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(accountName) ?? throw new ArgumentNullException(nameof(accountName));
            _ = StringHelpers.NullIfNullOrWhiteSpace(containerName) ?? throw new ArgumentNullException(nameof(containerName));
            _ = blobName ?? throw new ArgumentNullException(nameof(blobName));

            var uriBuilder = StorageAccountUriBuilderHelper(accountName, buildForBlob: true);

            // Container as path, optionally with blob.
            uriBuilder.Path = containerName;
            blobName = blobName.Trim();
            if (blobName.StartsWith('/'))
            {
                blobName = blobName.Substring(1);  // drop leading slash
            }

            if (blobName.Length != 0)
            {
                uriBuilder.Path = $@"{uriBuilder.Path}/{blobName}";
            }

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Construct a URI for a Storage Account
        /// </summary>
        /// <param name="accountName">The unqualified name of storage account (e.g., "myacct").
        /// No upper-case letters permited.</param>
        /// <param name="buildForBlobService">If true, build the URI for the blob service, rather than just the
        /// account.  e.g. "https://myacct.blob.core.windows.net" rather than "https://myacct.core.windows.net".
        /// </param>
        /// <exception cref="System.ArgumentNullException">Thrown if any of the arguments are null or either of
        /// accountName or containerName are strings only containing whitespace</exception>
        /// <exception cref="System.ArgumentException">Thrown if accountName contains any upper-case letters or
        /// special characters.  e.g., if "agcD" rather than "agcd"</exception>
        public static Uri BuildStorageAccountUri(string accountName, bool buildForBlobService = false)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(accountName) ?? throw new ArgumentNullException(nameof(accountName));

            var u = StorageAccountUriBuilderHelper(accountName, buildForBlob: buildForBlobService);

            return u.Uri;
        }

        /// <summary>
        /// Internal helper to construct a URI Builder for a Storage account.
        /// e.g., https://myacct.core.windows.net/ or https://myacct.blob.core.windows.net, depending on buildForBlob.
        /// </summary>
        /// <param name="accountName">The unqualified name of storage account (e.g., "myacct").
        /// No upper-case letters permited.</param>
        /// <param name="buildForBlob">If true, construct the Uri for the blob service.
        /// If false, the Uri should be for the storage account.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if any of the arguments are null or either of
        /// accountName or containerName are strings only containing whitespace</exception>
        /// <exception cref="System.ArgumentException">Thrown if accountName contains any upper-case letters or
        /// special characters.  e.g., if "agcD" rather than "agcd"</exception>
        private static UriBuilder StorageAccountUriBuilderHelper(string accountName, bool buildForBlob)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(accountName) ?? throw new ArgumentNullException(nameof(accountName));

            var accountNameTmp = accountName.Trim();

            if (accountNameTmp.Length == 0)
            {
                throw new ArgumentException($@"Storage account name '{accountName}' ({nameof(accountName)}) was empty.");
            }

            if (accountNameTmp.Length != accountName.Length)
            {
                throw new ArgumentException($@"Storage account name '{accountName}' ({nameof(accountName)}) had surrounding whitespace.");
            }

            // All lower-case?
            if (accountName != StringHelpers.ToLowerInvariant(accountName))
            {
                throw new ArgumentException($@"Storage account '{accountName}' must be in lower-case, no special characters.");
            }

            string flavor = buildForBlob ? StorageServiceConstants.AzureStorageDnsBlobSuffix : StorageServiceConstants.AzureStorageDnsSuffix;
            var urib = new UriBuilder(StorageServiceConstants.AzureStorageProtocol, $@"{accountName}.{flavor}");

            return urib;
        }

        /// <summary>
        /// Adjust the BlobSasBuilder to remove any '%' escapes within the container and blobName.
        ///
        /// This is needed to allow the SAS computation to be done correctly as the BlobSasBuilder
        /// class does not remove escapes, but the Azure Storage REST APIs do before SAS verification.
        /// So this is all to paper over that the SDK BlobSasBuilder appears (i.e., not in docs) to
        /// expect an unescaped URL input to the computation.   Azure REST however always undoes
        /// a single level of % escapes in the path portion of the URL (i.e. container, blob).
        /// We're aligning the SAS generation with the way the Azure Storage REST APIs perform
        /// SAS key verification.
        ///
        /// Important: Since the Azure Storage REST APIs do not process '+' as a synonym for space
        /// (or %20), this function does not process it as an escape.  It is possible to have a blob
        /// with '+' in the path/name, so this is left alone since it must be in the path going into
        /// the SAS key calculation.
        /// </summary>
        ///
        /// <remarks>
        /// Note: the issue arose due to spaces in blob names arriving from Requestors (as '+' or %20)
        /// and an ask to support the full set of blob names (e.g. for Japanese naming).  These
        /// extended characters will either show up in payloads either as UTF8-encoded bytes or as
        /// explicit escapes (e.g., 2-4 %nn tokens).  This function will handle either.
        ///
        /// Also, a related case of input specifying common characters via escapes works.
        /// e.g. “Fred” and “Fr%65d” need to be treated analogously.
        /// </remarks>
        public static BlobSasBuilder UnescapeTargetPath(this BlobSasBuilder bubIn)
        {
            _ = bubIn ?? throw new ArgumentNullException(nameof(bubIn));

            // either the container or the blob name could contain escapes, so undo those.

            string containerName = bubIn.BlobContainerName?.Trim() ?? string.Empty;
            if (containerName.Length > 0)
            {
                containerName = Uri.UnescapeDataString(containerName);
            }
            bubIn.BlobContainerName = containerName;

            string blobName = bubIn.BlobName?.Trim() ?? string.Empty;
            if (blobName.Length > 0)
            {
                blobName = Uri.UnescapeDataString(bubIn.BlobName);
            }
            bubIn.BlobName = blobName;

            return bubIn;
        }

        /// <summary>
        /// Adjust the Uri to remove any escapes (e.g., %3F for '?') within the non-query
        /// portion of the Uri and build an equivalent Uri where the URI-escaping (%HH)
        /// sequences are as minimized as possible to have the URI's AbsoluteUri property
        /// meet the UTF-8 format expectations of the Azure Storage Front end for the URI.
        /// This is in accordance with RFC 2277 (https://tools.ietf.org/html/rfc2277)
        ///
        /// Note that this purposely does not process the plus ('+') character as a synonym
        /// for space.  Thus, whether written as '+' or %2B, it will turn up as an
        /// unescaped plus character in the final URI.
        /// </summary>
        /// <param name="uriIn">The Uri instance to be adjusted.</param>
        /// <returns>
        /// A new <see cref="System.Uri"> instance with the result of making these changes to the input Uri.
        /// </returns>
        /// <remarks>
        /// Why not process the whole Uri?
        ///
        /// The driving issue relates to SAS computation not handling escapes as expected.
        /// In StorageService.GetSasUri(), the processing passes the input Uri through:
        ///       Uri -> BlobUriBuilder -> BlobSasBuilder
        /// and having the last compute the SAS key.  Escape sequences within the initial
        /// Uri are legal, but not handled by even the BlobUriBuilder correctly (e.g., try
        /// an extreme URL like: "https://myAcct.blob.core.windows.net/con%2Fa/b%2Fc.txt").
        ///
        /// So we know the path should be safe to "un-escape" the path portion, but best to
        /// leave the query portion as-is.  Both over-escaping and under-escaping can each
        /// cause problems, so the code takes the safe route.
        ///
        /// But an overarching consideration is that we're using this only for URIs being
        /// used for SAS computations and that does not even examine the query portion.
        /// </remarks>
        public static Uri NormalizeUriEscaping(this Uri uriIn)
        {
            _ = uriIn ?? throw new ArgumentNullException(nameof(uriIn));

            // Safe to pull everything up to the query/fragment.  We'll unescape it and
            // re-escape it. That will make any simple escapes (e.g. %2F for '/') return
            // to their simpler non-escaped form while still giving %HH escapes for any
            // "tough" characters (i.e., ones that aren't single byte ASCII).

            // 1. Everything up to the query/fragment
            UriComponents allButQuery = UriComponents.KeepDelimiter | UriComponents.SchemeAndServer | UriComponents.Path;
            var unescapedPrefixAndPath = uriIn.GetComponents(allButQuery, UriFormat.Unescaped);
            var lhs = Uri.EscapeUriString(unescapedPrefixAndPath);

            // 2. Query/Fragment, if any.
            //
            // Note that this can't use GetComponents() because it does not have the option
            // of "leave escaping as is".  We don't want to touch the escaping in these
            // portions because they'll already be escaped, and if we did an unescape/escape
            // sequence, things like %3D ('=') could get confused in the query string.  So
            // we just want to pull it for recomposition.
            var rhs = uriIn.Query;

            // Drop any trailing '/'
            if (lhs.EndsWith('/'))
            {
                lhs = lhs.Substring(0, lhs.Length - 1);
            }

            var resUri = new Uri(lhs + rhs);
            return resUri;
        }
    }

    /// <summary>Just a "DTO" with a pair of DateTimeOfsets</summary>
    public class TimeRange
    {
        /// <summary>Gets start of time span, backed up to cover possible clock skew.</summary>
        public DateTimeOffset StartTime { get; private set; }

        /// <summary>Gets end of time span, from Now + TTL</summary>
        public DateTimeOffset EndTime { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRange"/> class.
        /// </summary>
        /// <param name="backUpSecondsFromNow">backUpSecondsFromNow</param>
        /// <param name="startTime">startTime</param>
        /// <param name="ttl">ttl</param>
        internal TimeRange(int backUpSecondsFromNow, DateTimeOffset startTime, TimeSpan ttl)
        {
            this.StartTime = startTime.AddSeconds(-backUpSecondsFromNow);
            this.EndTime = startTime + ttl;
        }
    }
}

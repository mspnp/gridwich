using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// AccessTier isn't really an enum, so we have to resort to these kind of tricks to make it work.
    /// </summary>
    public class BlobAccessTier
    {
        /// <summary>
        /// Gets value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets Archive.
        /// </summary>
        public static readonly BlobAccessTier Archive = new BlobAccessTier(AccessTier.Archive.ToString());

        /// <summary>
        /// Gets Cool.
        /// </summary>
        public static readonly BlobAccessTier Cool = new BlobAccessTier(AccessTier.Cool.ToString());

        /// <summary>
        /// Gets Hot.
        /// </summary>
        public static readonly BlobAccessTier Hot = new BlobAccessTier(AccessTier.Hot.ToString());

        /// <summary>
        /// Gets ValidTiers.
        /// </summary>
        public static readonly Dictionary<BlobAccessTier, AccessTier> ValidTiers = new Dictionary<BlobAccessTier, AccessTier>
        {
            { Archive, AccessTier.Archive },
            { Cool, AccessTier.Cool },
            { Hot, AccessTier.Hot },
        };

        private BlobAccessTier(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Look up the BlobAccessTier by name.
        /// </summary>
        /// <param name="tierName">
        /// The BlobAccessTier name string that was used to construct the object instance.
        /// This matches the ToString() value.</param>
        /// <remarks>
        /// The matching is currently relaxed in terms of casing -- so "hOt" will match "Hot".
        /// </remarks>
        /// <returns>The corresponding BlobAccessTier instance or, if no matches, then null.</returns>
        public static BlobAccessTier Lookup(string tierName)
        {
            foreach (var tier in ValidTiers.Keys)
            {
                if (string.Equals(tierName, tier.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return tier;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets Value as string.
        /// </summary>
        /// <returns>Value as string.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}

using Azure.Storage.Blobs.Models;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Wrapper enum for RehydratePriority used to avoid leaking any possible new
    /// SDK Rehydration values in the Events exposed to the External System.
    /// </summary>
    /// <remarks>
    /// Note that this is not an 1:1 mapping, so you can't really convert a
    /// RehydratePriority to BlobRehydratePriority as there is ambiguity in the values.
    /// </remarks>
    public enum BlobRehydratePriority
    {
        /// <summary>
        /// Likely the fastest completion time.
        /// </summary>
        High = RehydratePriority.High,

        /// <summary>
        /// The value used as the default priority.
        /// The value of this enumerator should be that of another in
        /// this enumeration -- corresponding to the blob rehydration
        /// priority level you wish Azure Storage to use as the Gridwich default.
        /// </summary>
        Normal = Low,

        /// <summary>
        /// Least expensive, potentially slower completion time.
        /// </summary>
        Low = RehydratePriority.Standard,
    }
}

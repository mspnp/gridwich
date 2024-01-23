using Azure.Storage.Blobs.Models;
using System;

namespace Gridwich.SagaParticipants.Analysis.MediaInfoTests.Utils
{
    public static class MediaInfoTestUtils
    {
        public static BlobProperties CreateBlobProperties(long contentLength)
        {
            return CreateBlobProperties(default, default, default, default, contentLength);
        }

        private static BlobProperties CreateBlobProperties(
            DateTimeOffset lastModified = default,
            LeaseDurationType leaseDuration = default,
            LeaseState leaseState = default,
            LeaseStatus leaseStatus = default,
            long contentLength = default,
            string destinationSnapshot = default,
            Azure.ETag eTag = default,
            byte[] contentHash = default,
            string contentEncoding = default,
            string contentDisposition = default,
            string contentLanguage = default,
            bool isIncrementalCopy = default,
            string cacheControl = default,
            CopyStatus copyStatus = default,
            long blobSequenceNumber = default,
            Uri copySource = default,
            string acceptRanges = default,
            string copyProgress = default,
            int blobCommittedBlockCount = default,
            string copyId = default,
            bool isServerEncrypted = default,
            string copyStatusDescription = default,
            string encryptionKeySha256 = default,
            DateTimeOffset copyCompletedOn = default,
            string accessTier = default,
            BlobType blobType = default,
            bool accessTierInferred = default,
            System.Collections.Generic.IDictionary<string, string> metadata = default,
            string archiveStatus = default,
            DateTimeOffset createdOn = default,
            DateTimeOffset accessTierChangedOn = default,
            string contentType = default)
        {
            return BlobsModelFactory.BlobProperties(
                lastModified,
                leaseDuration,
                leaseState,
                leaseStatus,
                contentLength,
                destinationSnapshot,
                eTag,
                contentHash,
                contentEncoding,
                contentDisposition,
                contentLanguage,
                isIncrementalCopy,
                cacheControl,
                copyStatus,
                blobSequenceNumber,
                copySource,
                acceptRanges,
                copyProgress,
                blobCommittedBlockCount,
                copyId,
                isServerEncrypted,
                copyStatusDescription,
                encryptionKeySha256,
                copyCompletedOn,
                accessTier,
                blobType,
                accessTierInferred,
                metadata,
                archiveStatus,
                createdOn,
                accessTierChangedOn,
                contentType);
        }
    }
}
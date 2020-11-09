using System;
using Azure.Storage.Blobs;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <inheritdoc/>
    public class StorageContainerClientSleeve : IStorageContainerClientSleeve
    {
        // Implementation note:
        //      It is important that runtime updates to fields within the Context are done
        //      in-place (thus no public set).  i.e., change the individual fields/props values
        //      within Context, not the Context instance referred to by a sleeve.
        //
        //      The reason for this is that, during the creation of this sleeve, an HTTP pipeline
        //      instance (see Gridwich.Services.BlobContainerClientProvider.CreateBlobClientSleeveForUri)
        //      was associated with the Client object.  That pipeline instance reaches back into
        //      this exact Context instance at runtime to push/pull the Operation Context and ETag.

        /// <inheritdoc/>
        public BlobContainerClient Client { get; }

        /// <inheritdoc/>
        public BlobServiceClient Service { get; }

        /// <inheritdoc/>
        public StorageClientProviderContext Context { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContainerClientSleeve" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="service">The service.</param>
        /// <param name="context">The context.</param>
        /// <exception cref="ArgumentException">@"client.AccountName is invalid - client</exception>
        public StorageContainerClientSleeve(BlobContainerClient client, BlobServiceClient service, StorageClientProviderContext context)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));

            if (string.IsNullOrWhiteSpace(client.AccountName))
            {
                throw new ArgumentException($@"client.AccountName is invalid", nameof(client));
            }

            Context = context ?? throw new ArgumentNullException(nameof(context));
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// True if the sleeve matches the same Operational Context as the argument.
        /// </summary>
        /// <param name="context">The context to compare against.</param>
        /// <returns>
        ///   <c>true</c> if this is part of same request for the specified context; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPartOfSameRequest(StorageClientProviderContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            return Context.ClientRequestID == context.ClientRequestID;
        }
    }
}
using System;
using Azure.Core;
using Azure.Core.Pipeline;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <summary>
    /// A pipeline policy used by the BlobBaseClient and BlobContainerClient instances.
    /// The purpose of having this policy is to load headers into requests and extract
    /// the corresponding values from responses.  In particular:<ul>
    ///   <li>Gridwich OperationContext (HOC) - serialized for ClientRquestId header
    ///   <li>ETag - if non-blank, propogate current value through request/response/request...
    /// </ul>
    /// </summary>
    /// <remarks>
    /// Each BlobBaseClient will have it's own instance of this policy, and the
    /// policy is set to be invoked on every request/response.
    ///
    /// Because the policy is inaccessible once the BlobBaseClient has been created, it
    /// maintains HOC and ETag values via an BlobClientProviderContext instance. It is
    /// important that updates to context values (e.g. you want to reuse an existing
    /// BlobBaseClient instance for a different HOC value), are done by changing the
    /// values within that same BlobClientProviderContext instance, rather than by
    /// just setting the sleeve to point at another.  This class does not interact
    /// with StorageBlobClientSleeves.
    /// </remarks>
    public class BlobClientPipelinePolicy : HttpPipelineSynchronousPolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobClientPipelinePolicy"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public BlobClientPipelinePolicy(StorageClientProviderContext context)
        {
            this.context = context;  // important that we hold a reference, not make a copy.
        }

        /// <summary>A reference to the current context values</summary>
        private readonly StorageClientProviderContext context;

        /// <summary>
        /// Plug-in values for each request.
        /// </summary>
        /// <param name="message">The <see cref="HttpMessage" /> containing the request.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public override void OnSendingRequest(HttpMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!string.IsNullOrEmpty(context.ClientRequestID))
            {
                message.Request.ClientRequestId = context.ClientRequestID;
            }
            if (context.TrackingETag)
            {
                if (!string.IsNullOrEmpty(context.ETag))
                {
                    message.Request.Headers.SetValue(Microsoft.Net.Http.Headers.HeaderNames.ETag, context.ETag);
                }
            }

            // Workaround for AzStorage SDK Bug: https://github.com/Azure/azure-sdk-for-net/issues/11368
            //
            // Background:
            //
            // The REST call for CopyBlob expects the source URI to be placed on
            // an HTTP header called "x-ms-copy-source".  The issue is that the SDK (at least as of V12)
            // is undoing %hh escapes present in the URL.  The URL is presumed to be UTF-8 encoded, so
            // those escapes represent the 2-4 byte hex sequences UTF-8 uses to represent non-ASCII
            // characters.  The result is that we now have an HTTP header that may contain non-ASCII
            // characters (which fails in the REST API).  The net is that the escapes need to be
            // kept, so the code below re-encodes the result via AbsoluteUri.
            //
            // When this "unescaping" issue is fixed in a later SDK, the code below could be removed,
            // but need not be.  In the absence of the problem, this will still produce the correct
            // result for the header.

            string value;
            const string SourceHeader = "x-ms-copy-source";

            if (message.Request.Headers.TryGetValue(SourceHeader, out value))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Uri valueUri = new Uri(value);
                    message.Request.Headers.SetValue(SourceHeader, valueUri.AbsoluteUri);
                }
            }

            base.OnSendingRequest(message);
        }

        /// <summary>
        /// Extract values from each response.
        /// </summary>
        /// <param name="message">The <see cref="HttpMessage" /> containing the response.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public override void OnReceivedResponse(HttpMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            // Note: no need to save the returned clientRequestID since
            // it shouldn't change from what we sent

            // But the ETag should always be retained for subsequent requests
            // and/or inspection.

            var etag = message.Response.Headers.ETag;

            if (etag.HasValue)
            {
                string val = etag.Value.ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    context.ETag = val;
                }
            }

            base.OnReceivedResponse(message);
        }
    }
}

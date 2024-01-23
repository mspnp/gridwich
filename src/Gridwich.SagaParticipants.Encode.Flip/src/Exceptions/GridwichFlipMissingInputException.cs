using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when a Flip encode cannot find one or more input blobs.
    /// </summary>
    [type: SuppressMessage("Microsoft.Design", "CA1054:URI parameters should not be strings", Justification = "Intent is clearer with no extra overloads.")]
    public class GridwichFlipMissingInputException : GridwichException
    {
        /// <summary>The key in the exception data under which the problem URI string is stored.</summary>
        public const string UriKey = "MissingUri";

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipMissingInputException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="missingUrl">The URL of the blob that couldn't be found</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichFlipMissingInputException(string message, string missingUrl, JObject operationContext)
         : base(message, LogEventIds.CloudPortSASError, operationContext)
        {
            StoreUri(missingUrl);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipMissingInputException" /> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="missingUrl">The URL of the blob that couldn't be found</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public GridwichFlipMissingInputException(string message, string missingUrl, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.CloudPortSASError, operationContext, innerException)
        {
            StoreUri(missingUrl);
        }

        // <summary>Save the problem Uri with the exception.</summary>
        private void StoreUri(string missingUrl)
        {
            // Store the problem URI with the exception.
            string s = missingUrl ?? string.Empty;
            this.SafeAddToData(UriKey, s);
        }
    }
}
using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationMissingManifestFileException"/>.
    /// </summary>
    public class GridwichPublicationMissingManifestFileException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationMissingManifestFileException"/> class.
        /// </summary>
        /// <param name="containerUri">The container uri which does not have a manifest file.</param>
        /// <param name="message">The base exception message you want to set.</param>
        public GridwichPublicationMissingManifestFileException(Uri containerUri, string message)
         : base(message, LogEventIds.MediaServicesV3AttemptToGetNonexistentManifest, null)
        {
            SafeAddToData("containerUri", containerUri);
        }
    }
}

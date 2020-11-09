using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the CloudPort encoder.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information

        /// <summary>Information: CloudPort progress message</summary>
        public static readonly EventId CloudPortProgress = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Information, 0,
            "CloudPort encode progress.");

        /// <summary>Information: This encoder does not handle this encode type</summary>
        public static readonly EventId CloudPortDoesNotHandleThisType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Information, 1,
            "This encoder does not handle this encode type.");

        /// <summary>Information: CloudPort encoder success</summary>
        public static readonly EventId CloudPortSuccess = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Information, 2,
            "CloudPort encode successful.");

        // Error

        /// <summary>Error: Invalid parameters sent on Encode create</summary>
        public static readonly EventId CloudPortParameterError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Error, 0,
            "Invalide parameters send on Encode create.");

        /// <summary>Error: Workflow or preset does not exist</summary>
        public static readonly EventId CloudPortWorkflowDoesNotExist = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Error, 1,
            "Workflow or preset does not exist.");

        // Critical

        /// <summary>Critical: Null or Invalid API_KEY</summary>
        public static readonly EventId CloudPortApiError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 0,
            "Null or Invalid API_KEY in CloudPortClientProvider.");

        /// <summary>Critical: CloudPort missing input error</summary>
        public static readonly EventId CloudPortMissingInputError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 1,
            "CloudPort missing input error.");

        /// <summary>Critical: "Error SAS Uri assets</summary>
        public static readonly EventId CloudPortSASError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 2,
            "Error SAS Uri assets.");

        /// <summary>Critical: Cannot create Vantage Storage Reference. API error</summary>
        public static readonly EventId CloudPortCannotCreateStorageReferenceAPIError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 3,
            "CloudPort error, unable to create storage reference.  Error calling the CloudPort API.");

        /// <summary>Critical: Cannot create Vantage Storage Reference. Unknown error</summary>
        public static readonly EventId CloudPortCannotCreateStorageReferenceUnknownError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 4,
            "CloudPort error, unable to create storage reference.  Unknown error.");

        /// <summary>Critical: CloudPort error, unable to find account key for storage account in Key Vault</summary>
        public static readonly EventId CloudPortCannotFindStorageAccountKeyError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 5,
            "CloudPort error, unable to find account key for storage account in Key Vault.");

        /// <summary>Critical: Error deleting Telestream Cloud storage references</summary>
        public static readonly EventId CloudPortDeletingStorageReferencesError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.CloudPort, LogEventIdLevel.Critical, 6,
            "Error deleting Telestream Cloud storage references.");
    }
}
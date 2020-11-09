using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Media Services V3 Publication classes.
    /// </summary>
    public static partial class LogEventIds
    {
        // Warning

        /// <summary>Information: Conflict when creating or updating the content key policy</summary>
        public static readonly EventId MediaServicesV3ContentKeyPolicyConflict = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Warning, 0,
            "Conflict when creating/updating the content key policy.");

        // Error

        /// <summary>Error: The requested streamingPolicy is not supported</summary>
        public static readonly EventId PublicationStreamingPolicyNotSupported = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Error, 0,
            "The requested streamingPolicy is not supported.");

        /// <summary>Error: The requested contentKeyPolicy is not supported</summary>
        public static readonly EventId PublicationContentKeyPolicyNotSupported = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Error, 1,
            "The requested contentKeyPolicy is not supported.");

        /// <summary>Error: Error with Media Services V3 Configuration</summary>
        public static readonly EventId MediaServicesV3ConfigurationError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Error, 2,
            "Error with Media Services V3 Configuration.");

        /// <summary>Error: Error when creating/updating the content key policy</summary>
        public static readonly EventId MediaServicesV3ContentKeyPolicyCreateUpdateError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Error, 3,
            "Error when creating/updating the content key policy.");


        // Critical

        /// <summary>Critical: Error when creating/getting asset</summary>
        public static readonly EventId FailedToCreateOrGetAsset = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 0,
            "Error when creating/getting asset.");

        /// <summary>Critical: Error when creating asset filter</summary>
        public static readonly EventId FailedToCreateAssetFilter = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 1,
            "Error when creating asset filter.");

        /// <summary>Critical: Error when creating locator</summary>
        public static readonly EventId FailedToCreateLocator = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 2,
            "Error when creating locator.");

        /// <summary>Critical: Error when listing the streaming endpoints in the account</summary>
        public static readonly EventId FailedToListStreamingEndpoints = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 3,
            "Error when listing the streaming endpoints in the account.");

        /// <summary>Critical: Error when listing the streaming paths</summary>
        public static readonly EventId FailedToListStreamingPaths = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 4,
            "Error when listing the streaming paths.");

        /// <summary>Critical: Error when deleting locator</summary>
        public static readonly EventId FailedToDeleteLocator = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 5,
            "Error when deleting locator.");

        /// <summary>Critical: Error in specied time. </summary>
        public static readonly EventId TimeParameterOutOfRange = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 6,
            "Error parsing specified time.");

        /// <summary>Critical: Attempt to get nonexistent manifest in blob container. </summary>
        public static readonly EventId MediaServicesV3AttemptToGetNonexistentManifest = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Publisher, LogEventIdLevel.Critical, 7,
            "Attempt to get nonexistent manifest in blob container.");
    }
}
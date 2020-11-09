using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Media Services V2.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information

        /// <summary>Information: The Media Services V2 encoder has successfully created a new asset</summary>
        public static readonly EventId MediaServicesV2AssetCreated = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 0,
            "The Media Services V2 encoder has successfully created a new asset.");

        /// <summary>Information: The Media Services V2 encoder has successfully submitted a job</summary>
        public static readonly EventId MediaServicesV2JobSubmitCalled = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 1,
            "The Media Services V2 encoder has successfully submitted a job.");

        /// <summary>Information: The Media Services V2 service successfully copied the file</summary>
        public static readonly EventId MediaServicesV2CopyFileCompleted = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 2,
            "The Media Services V2 service successfully copied the file.");

        /// <summary>Information: The Media Services V2 service has successfully copied a file and updated the asset</summary>
        public static readonly EventId MediaServicesV2CopyFileAndUpdateAssetSuccess = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 3,
            "The Media Services V2 service has successfully copied a file and updated the asset.");

        /// <summary>Information: AMS V2 Callback function triggered</summary>
        public static readonly EventId CallbackFunctionTriggered = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 4,
            "AMS V2 Callback function triggered.");

        /// <summary>Information: AMS V2 Callback function has processed the notification message</summary>
        public static readonly EventId CallbackFunctionNotificationMessageProcessed = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 5,
            "AMS V2 Callback function has processed the notification message.");

        /// <summary>Information: The request to AMS V2 is missing the VerifyWebHookRequestSignature header</summary>
        public static readonly EventId RequestIsMissingVerifyWebHookRequestSignature = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Information, 6,
            "The request to AMS V2 is missing the VerifyWebHookRequestSignature header.");

        // Warning

        /// <summary>Warning: The message did not match an event type that can be handled by this service</summary>
        public static readonly EventId MediaServicesV2UnableToHandleMessageEventType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Warning, 0,
            "The message did not match an event type that can be handled by this service.");

        // Error

        /// <summary>Error: MediaServicesV2EncoderSpecificData in error</summary>
        public static readonly EventId MediaServicesV2SpecificDataError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 0,
            "MediaServicesV2SpecificData in error.");

        /// <summary>Error: Error with Media Services V2 input Uri(s)</summary>
        public static readonly EventId MediaServicesV2InputError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 1,
            "Error with Media Services V2 input Uri(s).");

        /// <summary>Error: Error connecting to Media Services V2</summary>
        public static readonly EventId MediaServicesV2ConnectionError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 3,
            "Error connecting to Media Services V2.");

        /// <summary>Error: Error when updating/creating Media Services V2 input asset</summary>
        public static readonly EventId MediaServicesV2InputAssetError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 4,
            "Error when updating/creating Media Services V2 input asset.");

        /// <summary>Error: Error when getting/creating Media Services V2 preset</summary>
        public static readonly EventId MediaServicesV2PresetError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 5,
            "Error when getting/creating Media Services V2 preset.");

        /// <summary>Error: Error when creating Media Services V2 job</summary>
        public static readonly EventId MediaServicesV2CreateJobError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 6,
            "Error when creating Media Services V2 job.");

        /// <summary>Error: Error with Media Services V2 Configuration</summary>
        public static readonly EventId MediaServicesV2ConfigurationError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 7,
            "Error with Media Services V2 Configuration.");

        /// <summary>Error: Error deleting MediaServicesV2 asset</summary>
        public static readonly EventId MediaServicesV2DeleteError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 8,
            "Error deleting MediaServicesV2 asset.");

        /// <summary>Error: The Media Services V2 service copy and update asset failed</summary>
        public static readonly EventId MediaServicesV2CopyFileAndUpdateAssetError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 9,
            "The Media Services V2 service has failed to copy a file and update the asset.");

        /// <summary>Error: The Media Services V2 service failed to get the operation context for a job</summary>
        public static readonly EventId MediaServicesV2FailedToGetJobOpContext = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 10,
            "The Media Services V2 service failed to get the operation context for a job.");

        /// <summary>Error: The Media Services V2 service failed to parse output container</summary>
        public static readonly EventId MediaServicesV2FailedToParseOutputContainer = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 11,
            "The Media Services V2 service failed to parse output container.");

        /// <summary>Error: The Media Services V2 service failed to get processor</summary>
        public static readonly EventId MediaServicesV2FailedToGetProcessor = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 12,
            "The Media Services V2 service failed to get processor.");

        /// <summary>Error: The Media Services V2 service found an error with the correlationData</summary>
        public static readonly EventId MediaServicesV2CorrelationDataError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 13,
            "The Media Services V2 service found an error with the correlationData.");

        /// <summary>Error: The Media Services V2 service failed to submit the mes job</summary>
        public static readonly EventId MediaServicesV2SubmitMesJobFailure = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 14,
            "The Media Services V2 service failed to submit the mes job.");

        /// <summary>Error: The Media Services V2 service failed to copy output asset</summary>
        public static readonly EventId MediaServicesV2CopyOutputAssetError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 15,
            "The Media Services V2 service failed to copy output asset.");

        /// <summary>Error: Could not find LastComputedProgress property in notification message</summary>
        public static readonly EventId MediaServicesV2PropertyNotFoundInNotificationMessageError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 16,
            "Could not find LastComputedProgress property in notification message.");

        /// <summary>Error: Could not parse progress from message property</summary>
        public static readonly EventId MediaServicesV2ParseProgressFailed = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 17,
            "Could not parse progress from message property.");

        /// <summary>Error: An exception was found while processing the message</summary>
        public static readonly EventId MediaServicesV2MessageProcessingFailed = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 18,
            "An exception was found while processing the message.");

        /// <summary>Error: Attempt to use nonexistent blob as input</summary>
        public static readonly EventId MediaServicesV2AttemptToUseNonexistentBlobAsInput = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV2, LogEventIdLevel.Error, 19,
            "Attempt to use nonexistent blob as input source.");
    }
}
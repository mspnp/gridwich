using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Media Services V3.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information

        /// <summary>Information: The Media Services V3 encoder has successfully submitted a job</summary>
        public static readonly EventId MediaServicesV3JobSubmitCalled = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Information, 0,
            "The Media Services V3 encoder has successfully submitted a job.");

        // Error

        /// <summary>Error: Error with Media Services V3 encode create dto</summary>
        public static readonly EventId MediaServicesV3EncodeCreateDtoError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 0,
            "Error with Media Services V3 encode create dto.");

        /// <summary>Error: Error with Media Services V3 output Uri</summary>
        public static readonly EventId MediaServicesV3OutputError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 1,
            "Error with Media Services V3 output Uri.");

        /// <summary>Error: Error connecting to Media Services V3</summary>
        public static readonly EventId MediaServicesV3ConnectionError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 2,
            "Error connecting to Media Services V3.");

        /// <summary>Error: Error connecting to Media Services V3 with Active Directory Authentication Library (ADAL)</summary>
        public static readonly EventId MediaServicesV3ConnectionAdalError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 3,
            "Error connecting to Media Services V3 with Active Directory Authentication Library (ADAL).");

        /// <summary>Error: Error when updating/creating Media Services V3 input asset</summary>
        public static readonly EventId MediaServicesV3InputAssetError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 4,
            "Error when updating/creating Media Services V3 input asset.");

        /// <summary>Error: Error when getting/creating Media Services V3 transform</summary>
        public static readonly EventId MediaServicesV3TransformError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 5,
            "Error when getting/creating Media Services V3 transform.");

        /// <summary>Error: API error when creating Media Services V3 job</summary>
        public static readonly EventId MediaServicesV3CreateJobApiError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 6,
            "API error when creating Media Services V3 job.");

        /// <summary>Error: JobCanceled event received</summary>
        public static readonly EventId MediaServicesV3JobCanceledReceived = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 7,
            "JobCanceled event received.");

        /// <summary>Error: JobErrored event received</summary>
        public static readonly EventId MediaServicesV3JobErroredReceived = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 8,
            "JobErrored event received.");

        /// <summary>Error: The provided event type cannot be deserialized by this handler</summary>
        public static readonly EventId MediaServicesV3InvalidEventTypeError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 9,
            "The provided event type cannot be deserialized by this handler.");

        /// <summary>Error: Attempt to use nonexistent blob as input to AMS V3 encoding</summary>
        public static readonly EventId MediaServicesV3AttemptToUseNonexistentBlob = EventHelpers.CreateEventId(
            LogEventIdSubsystem.MediaServicesV3Encoder, LogEventIdLevel.Error, 10,
            "Attempt to use nonexistent blob as input to AMS V3 encoding.");
    }
}
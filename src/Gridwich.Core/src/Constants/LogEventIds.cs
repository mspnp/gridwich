using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;
using System;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains static members for all Events in Gridwich.
    /// Normally, each subsystem creates a seperate LogEventIds<subSystemName>.cs
    /// file in this Gridwich.Core.Constants extending the partial class definition
    /// of LogEventIds.
    ///
    /// The Event ID numbering scheme is captured in the
    /// <see cref="LogEventIdsIdEncoding"/> class (bottom of this file). The
    /// information and helper methods in that class are used by
    /// Gridwich.Core.Helpers.EventHelpers.CreateEventId() to compose IDs from<ul>
    /// <li>the id Level (e.g. warning, error, etc.)
    /// <see cref="LogEventIds.LogEventIdLevel"/>
    /// <li>the subsystem generating the event (e.g. StorageService)
    /// <see cref="LogEventIds.LogEventIdSubsystem"/>
    /// <li>A number for the specific event. A number in the range [0..LogEventIdsIdEncoding.MaxIndex],
    /// unique for the LogEventIdLevel/LogEventIdSubsystem pair.
    /// </ul>
    /// </summary>
    public static partial class LogEventIds
    {
        /// <summary>
        /// These are the Event Level portion of an ID.
        /// When displayed in decimal, this level digit will be the high-order portion of an eventID.
        /// </summary>
        /// <remarks>
        /// These start at zero and increase by 1.
        /// There can be no more than _DoNotExceed LogEventIdLevels (currently 10).
        /// </remarks>
        public enum LogEventIdLevel : int
        {
            /// <summary>
            /// Trace - Logs that contain the most detailed messages.These messages may contain
            /// sensitive application data.These messages are disabled by default and should never
            /// be enabled in a production environment.
            /// </summary>
            Trace = 1,

            /// <summary>
            /// Debug - Logs that are used for interactive investigation during development.
            /// These logs should primarily contain information useful for debugging and have no long-term value.
            /// </summary>
            Debug = 2,

            /// <summary>
            /// Information - Logs that track the general flow of the application.These logs should have long-term value.
            /// </summary>
            Information = 3,

            /// <summary>
            /// Warning - Logs that highlight an abnormal or unexpected event in the application flow,
            /// but do not otherwise cause the application execution to stop.
            /// </summary>
            Warning = 4,

            /// <summary>
            /// Error - Logs that highlight when the current flow of execution is stopped due to a failure.
            /// These should indicate a failure in the current activity, not an application-wide failure.
            /// </summary>
            Error = 5,

            /// <summary>
            /// Critical - Logs that describe an unrecoverable application or system crash, or a
            /// catastrophic failure that requires immediate attention.
            /// </summary>
            Critical = 6,

            /// <summary>
            /// The largest LogEventIdLevel enumerator value that should ever be added here in future.
            /// Special naming so it's obvious that it's not a normal enumerator.
            /// Do not change this enumerator name without also changing IsUpperLimtEnumerator()
            /// </summary>
            @SPECIALDoNotExceed = LogEventIdsIdEncoding.MaxLevel,
        }

        /// <summary>
        /// The Subsystem that is logging the event.
        /// We have generally chosen the implementation classes, but they can be arbitrary,
        /// and one class could have more than one subsystem.
        /// </summary>
        /// <remarks>
        /// Subsystems start at 0, for placeholders which are not used in production, and usually increment by 1.
        /// There can be no more than _DoNotExceed subsystems (currently 100).
        ///
        /// Note that there are intentionally gaps in the enumerator values below.  This is
        /// just an attempt to group function areas (e.g. storage-related are in the 20-39 series).
        /// There is no other significance and any new subsystem could be placed in any unused
        /// value in the sequence.
        ///
        /// Each subsytem will have a limit on the number of
        /// eventIds (per Level), falling in the range [0..LogEventIdsIdEncoding.MaxIndex]
        /// (currently 100).  So each subsystem could have that number of Warnings, that
        /// number of Trace events, etc.
        /// </remarks>
        public enum LogEventIdSubsystem : int
        {
            /// <summary>Placeholder: only used during development</summary>
            Placeholder = 0,

            /// <summary>App: The application overall</summary>
            App = 1,

            /// <summary>App: The event handler core</summary>
            EventHandlerCore = 2,

            /// <summary>App: general media delivery process</summary>
            Delivery = 3,

            /// <summary>Storage: Event Handlers</summary>
            StorageEventHandlers = 10,

            /// <summary>Storage: The Storage Service</summary>
            StorageService = 11,

            /// <summary>Storage: Blob metadata</summary>
            Metadata = 12,

            /// <summary>Storage: Blob Copy</summary>
            BlobCopy = 13,

            /// <summary>Storage: Blob Delete</summary>
            BlobDelete = 14,

            /// <summary>Encoders: The flip encoder</summary>
            Flip = 30,

            /// <summary>Encoders: The CloudPort service</summary>
            CloudPort = 31,

            /// <summary>Encoding: Analysis</summary>
            Analysis = 40,

            /// <summary>Encoding: Archive</summary>
            Archive = 41,

            /// <summary>Encoding: the encode Proxy</summary>
            EncodeProxy = 42,

            /// <summary>Encoding: The indexer</summary>
            Index = 43,

            /// <summary>Encoding : Publishing</summary>
            Publish = 44,

            /// <summary>Encoding: the Encode itself</summary>
            Encode = 45,

            /// <summary>Encoding: Fulfillment</summary>
            Fulfillment = 46,

            /// <summary>Media Services: MediaInfo</summary>
            MediaInfo = 50,

            /// <summary>
            /// The largest LogEventIdSubsystem enumerator value that should ever
            /// be added here in future.
            /// Special naming so it's obvious that it's not a normal enumerator.
            /// Do not change this enumerator name without also changing IsUpperLimtEnumerator()
            /// </summary>
            @SPECIALDoNotExceed = LogEventIdsIdEncoding.MaxSubsystem,
        }

        // These generic Placeholders should be replaced by specific event ids and descriptions in the code.
        // You can use them during first-drafts of code / code-reviews.

        /// <summary>Trace: General Trace trace</summary>
        public static readonly EventId PlaceholderTrace = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Trace, 0,
            "General Trace trace.");

        /// <summary>Debug: General Debug trace</summary>
        public static readonly EventId PlaceholderDebug = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Debug, 0,
            "General Debug trace.");

        /// <summary>Information: General Information trace</summary>
        public static readonly EventId PlaceholderInformation = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Information, 0,
            "General Information trace.");

        /// <summary>Warning: General Warning trace</summary>
        public static readonly EventId PlaceholderWarning = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Warning, 0,
            "General Warning trace.");

        /// <summary>Error: General Error trace</summary>
        public static readonly EventId PlaceholderError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Error, 0,
            "General Error trace.");

        /// <summary>Critical: General Critical trace</summary>
        public static readonly EventId PlaceholderCritical = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Placeholder, LogEventIdLevel.Critical, 0,
            "General Critical trace.");

        /// <summary>
        /// Gets the enumerator whose name starts with '@'.
        /// </summary>
        /// <param name="enumeratorName">enumeratorName.</param>
        /// <returns>Return true if the enumerator name is the special names used in the subsystem/level enumerations above to mark out the max possible enumerator value for that type.</returns>
        public static bool IsUpperLimtEnumerator(string enumeratorName)
        {
            _ = enumeratorName ?? throw new ArgumentNullException(nameof(enumeratorName));
            return enumeratorName.StartsWith('@');
        }

        /// <summary>Generate the highest possible event id for the subsystem/level pair of evt.</summary>
        /// <param name="evt">Event.</param>
        /// <returns>id.</returns>
        public static int GetMaxEventIdForRange(this EventId evt)
        {
            return GenerateId(evt.GetLevel(), evt.GetSubsystem(), LogEventIdsIdEncoding.MaxIndex);
        }

        /// <summary>Generate the lowest possible (index==0) event id for the subsystem/level pair of evt.</summary>
        /// <param name="evt">Event.</param>
        /// <returns>id.</returns>
        public static int GetMinEventIdForRange(this EventId evt)
        {
            return GenerateId(evt.GetLevel(), evt.GetSubsystem(), 0);
        }

        /// <summary>
        /// Gets the level as a LogEventIdLevel enumeration value for the given <c>EventId</c>.
        /// </summary>
        /// <param name="evt">The eventId</param>
        /// <returns>The enumerator value for <see cref="LogEventIds.LogEventIdLevel"/></returns>
        public static LogEventIds.LogEventIdLevel GetLevel(this EventId evt)
        {
            var enumValue = (evt.Id / LogEventIdsIdEncoding.MultiplierForLevel)
                            % (LogEventIdsIdEncoding.MaxLevel + 1);

            return (LogEventIds.LogEventIdLevel)enumValue;
        }

        /// <summary>
        /// Gets the level as a string for the given <c>EventId</c>.
        /// </summary>
        /// <param name="evt">The eventId</param>
        /// <returns>A string mapping to the enumeration value for <see cref="LogEventIds.LogEventIdLevel"/></returns>
        public static string GetLevelName(this EventId evt)
        {
            return evt.GetLevel().ToString();
        }

        /// <summary>
        /// Gets the subsystem as a LogEventIdSubsystem enumerator for the given <c>EventId</c>
        /// </summary>
        /// <param name="evt">The eventId</param>
        /// <returns>The enumerator value for <see cref="LogEventIds.LogEventIdSubsystem"/></returns>
        public static LogEventIds.LogEventIdSubsystem GetSubsystem(this EventId evt)
        {
            // Strip off the index portion, then mask off to get the subsystem.
            var enumValue = (evt.Id / LogEventIdsIdEncoding.MultiplierForSubsystem)
                                 % (LogEventIdsIdEncoding.MaxSubsystem + 1);

            return (LogEventIds.LogEventIdSubsystem)enumValue;
        }

        /// <summary>
        /// Gets the subsystem as a string for the given <c>EventId</c>
        /// </summary>
        /// <param name="evt">The eventId</param>
        /// <returns>A string mapping to the enumeration value for <see cref="LogEventIds.LogEventIdSubsystem"/></returns>
        public static string GetSubsystemName(this EventId evt)
        {
            return evt.GetSubsystem().ToString();
        }

        /// <summary>
        /// Gets the index portion of the eventID.id value for the given <c>EventId</c>
        /// </summary>
        /// <param name="evt">The eventId</param>
        /// <returns>A int with the eventID</returns>
        public static int GetIndex(this EventId evt)
        {
            return evt.Id % (LogEventIdsIdEncoding.MaxLevel + 1);
        }

        /// <summary>
        /// Calculate an EventId.id value from the 3 parts.
        /// </summary>
        /// <param name="level">The <see cref="LogEventIds.LogEventIdLevel"/> component of the value.</param>
        /// <param name="subSystem">The <see cref="LogEventIds.LogEventIdSubsystem"/> component of the value.</param>
        /// <param name="index">The index portion of the value.</param>
        /// <returns>A int containing the corresponding eventID value</returns>
        public static int GenerateId(LogEventIds.LogEventIdLevel level, LogEventIds.LogEventIdSubsystem subSystem, int index)
        {
            if (index < 0 || index > LogEventIdsIdEncoding.MaxIndex)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index),
                        $"{nameof(index)} of {index} is outside the permitted [0..{LogEventIdsIdEncoding.MaxIndex}] range.");
            }

            return ((int)subSystem * LogEventIdsIdEncoding.MultiplierForSubsystem)
                     + ((int)level * LogEventIdsIdEncoding.MultiplierForLevel)
                     + index;
        }

        /// <summary>
        /// This class provides the encoding/decoding information for EventIds,
        /// based on enumerations in LogEventIds (above), plus an index per message.  It
        /// It is used by the static extensions in LogEventIds.
        ///
        /// The current encoding scheme yields 6 decimal digits ABCDEF:<ul>
        /// <li>A is the id Level (e.g. warning, error, etc.)
        /// <see cref="LogEventIds.LogEventIdLevel"/>
        /// <li>BC are the subsystem generating the event (e.g. StorageService)
        /// <see cref="LogEventIds.LogEventIdSubsystem"/>
        /// <li>DEF are the specific event. A number in the range [0..LogEventIdsIdEncoding.MaxIndex],
        /// unique for the LogEventIdLevel/LogEventIdSubsystem pair.
        /// </ul>
        /// </summary>
        private static class LogEventIdsIdEncoding
        {
            /// <summary>Never more than [0..MaxIndex] EventIds in a Level/Subsystem pair</summary>
            public const int MaxIndex = 999;

            /// <summary>Subsystem values in <see cref="LogEventIds.LogEventIdSubsystem"/> must be this or less.</summary>
            public const int MaxSubsystem = 99;

            /// <summary>Shift multiplier for encoding subsystems (e.g. multiply by 1,000)</summary>
            public const int MultiplierForSubsystem = MaxIndex + 1;

            /// <summary>ID level (e.g Warning) values in <see cref="LogEventIds.LogEventIdLevel"/> must be this or less.</summary>
            public const int MaxLevel = 9;

            /// <summary>Shift multiplier for encoding id level (e.g. multiply by 100,000)</summary>
            public const int MultiplierForLevel = (MaxSubsystem + 1) * MultiplierForSubsystem;
        }
    }
}

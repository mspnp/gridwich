using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Gridwich.Core.Constants;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Event-related helpers
    /// </summary>
    /// <remarks>
    /// Note, this class formerly tracked each instantiation to complain about
    /// duplicates.  This has now been moved to a unit test in EventHelpersTests.cs
    /// that will examine all of the static members of the LoggingEventIds class
    /// to detect duplicates at test time.
    ///
    /// See Gridwich.Core/Constants/LogEventIds.cs.
    /// </remarks>
    public static class EventHelpers
    {
        /// <summary>
        /// Create a new Microsoft.Extensions.Logging.EventId instance.
        ///
        /// Note that this construction is preferred via static method, rather than
        /// directly via EventId constructor inline, in order to control the way
        /// the components of the Id are combined to produce a single integer ID value.
        /// i.e. let's combine them in one place, not in many.
        /// </summary>
        /// <param name="subsystem">The subsystem.</param>
        /// <param name="severity">The severity.</param>
        /// <param name="eventIndex">Index of the event. The range is currently [0..99].
        /// The upper limit is dictated by <see cref="LogEventIds.LogEventIdsIdEncoding.MaxLevel"/>
        /// </param>
        /// <param name="messageText">Descriptive text to associate with the
        /// subsystem/severity/eventIndex triple.  Intended to be human readable.</param>
        /// <returns>The new <see cref="EventId"/></returns>
        /// <remarks>Sequences of events usually start at 0 and monotonically increase - so 0, 1, 2...</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="eventIndex"> is negative or too large.
        /// See parameter description for range information.
        /// </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="messageText"> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="messageText"> is non-null, but either empty or all whitespace.</exception>
        public static EventId CreateEventId(
                LogEventIds.LogEventIdSubsystem subsystem,
                LogEventIds.LogEventIdLevel severity,
                int eventIndex,
                string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw (messageText == null) ?
                        new ArgumentNullException(nameof(messageText)) :
                        new ArgumentException($"{nameof(messageText)} must be a non-empty string", nameof(messageText));
            }

            // Now the id generation is contained within LogEventIds.  If
            // the eventIndex is too large (or negative), GenerateId will throw
            // an argument exception, which we'll just let propogate to the caller.
            //
            // In normal use, this caller would be the C# runtime performing static
            // initialization which will result in unit testing failing.
            // So fails at test time, as desired.

            var id = LogEventIds.GenerateId(severity, subsystem, eventIndex);

            return new EventId(id, messageText);
        }
    }
}

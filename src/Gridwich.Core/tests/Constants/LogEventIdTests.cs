using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Gridwich.CoreTests
{
    [ExcludeFromCodeCoverage]
    public class LogEventIdTests
    {
        [Fact]
        /// <summary>
        /// Ensure that no two EventIds in Gridwich.Core.Constants.LogEventIds have the same value.
        /// </summary>
        public void EnsureUniqueLogEventIDs()
        {
            var d = GetStaticFieldValues<EventId>(typeof(LogEventIds));
            var dict = new Dictionary<int, string>(d.Count);

            foreach (var fieldName in d.Keys)
            {
                var eventId = d[fieldName];
                var key = eventId.Id;

                if (dict.ContainsKey(key))
                {
                    // duplicate, so blow up
                    var eventNameForValue = dict[key];

                    var msg = $"In Gridwich.Core.Constants.LogEventIds, EventIds {eventNameForValue} and {fieldName} both have Id {key} but should be unique";
                    eventNameForValue.ShouldBe(fieldName, msg);
                }
                else
                {
                    dict[key] = fieldName;  // remember for next pass
                }
            }
        }

        private class RangeInfoData
        {
            public int LowEventId;
            public int HighEventId;
            public bool HaveSeenZeroEntry;
            public int EventCount;
        }

        [Fact]
        /// <summary>
        /// Ensure that, for any subSystem/severity combination that has any EventIds, that there is a zero entry.
        /// </summary>
        /// <remarks>
        /// This one is an offshoot of a past "tideness" review comment.  Early on, it was not specified whether message
        /// ranges were to start at zero or one.  e.g.
        /// <code>
        ///    public static readonly EventId FailedToDeserializeEventData = EventHelpers.CreateEventId(
        ///           LogEventIdSubsystem.Metadata, LogEventIdLevel.Error, 0, ...
        /// </code>
        /// should that be a zero or one for the first message in MetaData/Error?  This test aims to check that
        /// there is a zero EventId for any populated combinations of MetaData/Error.  Combinations that are
        /// not used at all (perhaps Storage/Warning) are not complained about.
        /// </remarks>
        public void EnsureZeroOriginEventIdsPerCategory()
        {
            var allEventIds = GetStaticFieldValues<EventId>(typeof(LogEventIds));
            // reuse the bump-table from EventHelpers -- it has all of the subsystem/level combinations as keys.
            var rangeSizes = GenerateRangeBracketsForEventIds<RangeInfoData>(
                    (lowIdForRange) => new RangeInfoData()
                    {
                        LowEventId = lowIdForRange,
                        HighEventId = new EventId(lowIdForRange).GetMaxEventIdForRange(),
                        EventCount = 0,
                        HaveSeenZeroEntry = false,
                    });

            // Walk through every EventId noting which used ranges
            // have any present and which have one with an index of zero.
            foreach (var eid in allEventIds)
            {
                var rangeKey = eid.Value.GetMinEventIdForRange();

                rangeSizes.ContainsKey(rangeKey).ShouldBeTrue(
                    $"EventId found for unknown level/subsystem. Name={eid.Key}, Id={eid.Value.Id}, Msg=\"{eid.Value.Name}\"");

                var entry = rangeSizes[rangeKey];
                entry.EventCount++;
                if (rangeKey == eid.Value.Id)
                {
                    entry.HaveSeenZeroEntry = true;
                }
            }

            // Have now processed all Events, check to see if there are any
            // non-empty ranges that have no zero index EventId
            var badRanges = new List<EventId>(20);

            foreach (var rs in rangeSizes)
            {
                if (rs.Value.EventCount == 0)
                {
                    continue; // Empty range
                }
                if (rs.Value.HaveSeenZeroEntry)
                {
                    continue; // Non-Empty range, but has zero entry
                }
                badRanges.Add(new EventId(rs.Key));
            }

            string msgStr = string.Empty;

            if (badRanges.Count > 0)
            {
                // active ranges without zero entry found, build up a complete message listing all.
                var sb = new StringBuilder(500);
                sb.AppendFormat(CultureInfo.InvariantCulture,
                        "{0} Gridwich.Constants.LogEventId Range{1} without zero index [",
                        badRanges.Count, (badRanges.Count > 0) ? "s" : string.Empty);
                bool needComma = false;

                // Sort by Subsystem, then Event Level -- to minimize source file visits when fixing detected errors.
                badRanges.Sort((lhs, rhs) =>
                {
                    int subsystemCompare = string.Compare(lhs.GetSubsystemName(), rhs.GetSubsystemName(), true, CultureInfo.InvariantCulture);
                    return (subsystemCompare != 0) ? subsystemCompare : string.Compare(lhs.GetLevelName(), rhs.GetLevelName(), true, CultureInfo.InvariantCulture);
                });

                foreach (var br in badRanges)
                {
                    if (needComma)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(br.GetSubsystemName())
                        .Append('/')
                        .Append(br.GetLevelName());

                    needComma = true;
                }
                sb.Append("]");
                msgStr = sb.ToString();
            }
            badRanges.Count.ShouldBe(0, msgStr);
        }

        /// <summary>
        /// Generate an IDictionary where Key is the minimum EventId.Id value for the combination
        /// of EventLevel+subSystem, and the value is the type <typeparamref name="T"/> instance
        /// returned from the <c>Func</c> argument <paramref name="generateValueFromEventId"/>.
        ///
        /// The notion is to create a dictionary with an entry for every combination of subsystem and
        /// eventLevel.  Uses include things like LogEventId tests housekeeping when scanning for any
        /// combinations that do not have a zero entry (see EnsureZeroOriginEventIdsPerCategory in
        /// Gridwich.Core/tests/Constants/LogEventIdTests.cs).
        /// </summary>
        /// <returns>The ranges.</returns>
        /// <typeparam name="T">The type of the value type which</typeparam>
        /// <param name="generateValueFromEventId">A <c>Func<int,T></c> called with the minimum id value for each combination of subsystem/level.  It is to return a corresponding <typeparamref name="T"/> instance which becomes the IDictionary value associated with the int id argument value it receives.</param>
        public static IDictionary<int, T> GenerateRangeBracketsForEventIds<T>(Func<int, T> generateValueFromEventId)
        {
            var subSystems = TestHelpers.GetEnumerators<LogEventIds.LogEventIdSubsystem, int>();
            var eventLevels = TestHelpers.GetEnumerators<LogEventIds.LogEventIdLevel, int>();

            var rangeInfo = new Dictionary<int, T>(subSystems.Count * eventLevels.Count);

            // Generate the index of the zero'th element of every eventLevel/subSystem combination.
            // Skip the enumerator values that are just there to indicated the theoretical upper
            // limit of the particular enumeration.
            foreach (var el in eventLevels)
            {
                if (!LogEventIds.IsUpperLimtEnumerator(el.Key))
                {
                    var level = (LogEventIds.LogEventIdLevel)el.Value;

                    foreach (var ss in subSystems)
                    {
                        if (!LogEventIds.IsUpperLimtEnumerator(ss.Key))
                        {
                            var id = LogEventIds.GenerateId(level, (LogEventIds.LogEventIdSubsystem)ss.Value, 0);
                            T val = generateValueFromEventId(id);
                            rangeInfo[id] = val;
                        }
                    }
                }
            }

            return rangeInfo;
        }


        /// <summary>
        /// Extract all the static TField fields from the TContainer type into a
        /// Dictionary keyed by the field name.
        /// </summary>
        /// <param name="containingClassType">The class (likely static) containing the static fields to
        /// be retrieved.</param>
        /// <typeparam name="TField">The type of the static fields of interest.</typeparam>
        /// <returns>
        /// A Dictionary where the key is the field name and the value is the
        /// field value as type Tfield.
        /// </returns>
        /// <remarks>
        /// Note that this cannot be changed to simply be parameterized by the two types
        /// due to C# not permitting static classes to participate as generic type arguments.
        /// </remarks>
        private static Dictionary<string, TField> GetStaticFieldValues<TField>(System.Type containingClassType)
        {
            return containingClassType
              .GetFields(BindingFlags.Public | BindingFlags.Static)
              .Where(f => f.FieldType == typeof(TField))
              .ToDictionary(f => f.Name,
                            f => (TField)f.GetValue(null));
        }
    }
}
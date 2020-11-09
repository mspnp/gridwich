using System;

using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Gridwich.CoreTests
{
    /// <summary>
    /// Test EventHelper.CreateEventId which is used for the static initialization of EventIDs
    /// exposed as static members of the Gridwich.Core.Constants.LogEventIds class.
    /// </summary>
    /// <remarks>
    /// These tests present challenges due to the fact that static initialization is performed
    /// "just-in-time" by the .NET runtime, combined with having other tests and Gridwich app code
    /// directly referencing EventIds from LogEventIds.  So, at execution time, the first time
    /// one of those LogEventIds members is referenced, the static initialization (via CreateEventId)
    /// is triggered for the entire class.
    ///
    /// As above, these tests are no necessarily the first to trigger the initialization.  Plus,
    /// for production use, CreateEventId does not check for duplicated IDs.  By observation, xUnit
    /// is running the 3 tests below in parallel, often in different processes, and static initialization
    /// is being executed in each process.  The end result is that if there is a duplication of IDs within
    /// LogEventIds, the duplication will be caught by one or more tests which reference LogEventIds. If
    /// the tests below happen to run first, they will trigger the failing Argument exception from
    /// CreateEventId().  If not, the exception will cause a failure in one of the other tests which happen
    /// to reference LogEventIds, triggering the initialiation.
    ///
    /// Note: One would expect that Xunit would provide ordering of test cases to make this more
    /// predictable.  Aside from that being largely antithetical to Xunit's philosophy, the closest thing
    /// would have been to have these tests defined to run in a TestCaseOrderer marked for non-parallel
    /// execution.  For this circumstance, that would have also defeated the purpose as Xunit insists on
    /// running all parallel tests before non-parallel ones.  Thus, the static initialization would have
    /// occurred before we get here.
    /// </remarks>
    public class EventHelpersTests
    {
        [Fact]
        /// <summary>
        /// Check that both EventHelper.CreateEventId and the EventId constructor proper create the same thing
        /// </summary>
        public void EventHelperShouldCreateExactSameEventViaAnyConstructor()
        {
            var subSystem = LogEventIds.LogEventIdSubsystem.Encode;
            var level = LogEventIds.LogEventIdLevel.Debug;
            var index = 22;
            var msgText = "Some interesting text";

            int computedId = ((int)level * 100000) + ((int)subSystem * 1000) + index;
            int ehComputedId = LogEventIds.GenerateId(level, subSystem, index);

            var ev1 = new EventId(computedId, msgText);
            var ev2 = EventHelpers.CreateEventId(subSystem, level, index, msgText);

            ehComputedId.ShouldBe(computedId, "LogEventIds.GenerateId != computed ID");
            ev1.GetLevelName().ShouldBe(ev2.GetLevelName());
            ev1.GetSubsystemName().ShouldBe(ev2.GetSubsystemName());
            ev1.Id.ShouldBe(ev2.Id);
            ev1.Name.ShouldBe(ev2.Name);
            ev1.GetHashCode().ShouldBe(ev2.GetHashCode());
        }

        [Theory]
        [InlineData(null, 0, "Something")]
        [InlineData(typeof(System.ArgumentOutOfRangeException), int.MaxValue, "Something")]
        [InlineData(typeof(System.ArgumentOutOfRangeException), -1, "Some Message")]
        [InlineData(typeof(System.ArgumentException), 2, "")]
        [InlineData(typeof(System.ArgumentNullException), 3, null)]
        [InlineData(typeof(System.ArgumentException), 4, "   ")]
        [InlineData(typeof(System.ArgumentException), 5, " ")]
        /// <summary>
        /// Check that both negative indices as well as empty/null messages are caught and
        /// Result in ArgumentExceptions.
        /// </summary>
        public void EventHelperShouldThrowExceptionsForBadArguments(System.Type exceptionType, int indexValue, string message)
        {
            bool wantException = exceptionType != null;
            var wrappedMsg = message == null ? "null" : $"\"{message}\"";

            try
            {
                EventHelpers.CreateEventId(LogEventIds.LogEventIdSubsystem.Analysis, LogEventIds.LogEventIdLevel.Error, indexValue, message);
            }
            catch (Exception e)
            {
                if (!wantException)
                {
                    e.ShouldBeNull($"Should not have thrown exception for indexValue={indexValue}, message={wrappedMsg}");
                }
                e.ShouldBeOfType(exceptionType);
                return;
            }

            if (wantException)
            {
                // we shouldn't have gotten here, expected an exception, but didn't get one
                "No Exception".ShouldBeNull($"Missing {exceptionType.FullName} exception for indexValue={indexValue}, message={wrappedMsg}");
            }
        }
    }
}

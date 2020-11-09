using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Gridwich.Core.Helpers;
using Gridwich.Core.Models;

using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Shouldly;
using Xunit;
using sc=Gridwich.Core.Models.StorageClientProviderContext;

namespace Gridwich.CoreTests.Models
{
    // NOte: this using saved a mountain of typing and helps test readability.
    // The current C# compiler handles qualification correctly when needed.
    // Stylecop has just not caught up yet.
    //
    // The disabling simply allows the "sc" short-hand to be used here and
    // below.  It must be done via pragma because the SupressMessage attribute
    // would have to be applied to a namespace and namespaces can't have
    // attributes.
    //
    // For lovers of mystery, the the odd web that StyleCop weaves (without
    // the override) includes:
    //  - you can't have both using for sc and TupleDS1 inside the namespace
    //  - you can't move TupleDS1 outside.
    //  - you can't use the shortform to define the TupleDS1 alias, independent
    //    of where you put the using.
    // In short, a mess, so disabling to allow for better test readability.
    //
    // When Stylecop eventually catches up to the compiler's C# level, these
    // pragmas can be removed.
    //
    #pragma warning disable SA1135 // Using directives must be qualified
    using TupleDS1=System.Tuple<string, bool?, sc, string, sc, System.Type>;
    #pragma warning restore SA1135 // Using directives must be qualified

    public class StorageClientProviderContextTests
    {
        public StorageClientProviderContextTests()
        {
            // ensure that our options are set up as the default for Serializers.
            JsonHelpers.SetupJsonSerialization();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        public sealed partial class TestDataGenerator
        {
            private const string Cx01Raw = "{   \"x\" :  2 , \"aaaaa\":\"def\" @T }";
            private const string Cx02Bad = "{ \"x\" }"; // bad json
            private static readonly string MutedProp = $"\"{sc.MutedPropertyName}\":1";
            private static readonly string TestGuid = Guid.NewGuid().ToString();
            private static readonly string TestGuidProp = $"\"{sc.GuidPropertyName}\":\"{TestGuid}\"";
            private static readonly string TestGuidMuted = $"{{{TestGuidProp},{MutedProp}}}";
            private static readonly string TestGuidUnMuted = $"{{{TestGuidProp}}}";
            private static readonly string Cx01 = Cx01Raw.Replace("@T", "");
            private static readonly string Cx01Muted = Cx01Raw.Replace("@T", $",{MutedProp}");
            private static readonly string Cx01c = Cx01.Replace(" ", "");  // compressed version

            private static readonly TupleDS1[] DataSet1 = new TupleDS1[]
            {
                // Fields: contextStringIn, muting, contextObjectIn, contextStringOut, contextObjectOut, exceptionTypeExpected

                // simple JSON, show output is compressed equivalent
                new TupleDS1(Cx01, null, null, Cx01c, null, null),
                // same with muted, via object
                new TupleDS1(Cx01, true, null, null, new sc(Cx01Muted), null),
                // simple JSON, show empty string becomes empty JObject
                new TupleDS1("", null, null, "{}", null, null),
                // simple JSON, show whitespace string is the same as empty one.
                new TupleDS1("    ", null, null, "{}", null, null),
                // show empty with muted works.
                new TupleDS1("  ", true, null, $"{{{MutedProp}}}", null, null),
                // show empty with muted works.
                new TupleDS1("", true, null, $"{{{MutedProp}}}", null, null),
                // show null gets treated like string.Empty
                new TupleDS1(null, null, null, "{}", null, null),
                // show null gets treated like string.Empty
                new TupleDS1(null, null, null, null, new sc("{}"), null),
                new TupleDS1(Cx02Bad, null, null, null, null, typeof(System.ArgumentOutOfRangeException)),
                // Guid with default mute
                new TupleDS1(TestGuid, null, null, null, new sc(JObject.Parse(TestGuidUnMuted)), null),
                // Guid with forced mute
                new TupleDS1(TestGuid, true, null, null, new sc(JObject.Parse(TestGuidMuted)), null),
                // Guid with forced unmute
                new TupleDS1(TestGuid, false, null, null, new sc(JObject.Parse(TestGuidUnMuted)), null),
            };
            public static IEnumerable<object[]> GetDataSet1()
            {
                foreach (var t in DataSet1)
                {
                    // An admittedly inelegant way to return the data, but Xunit insists on
                    // object[] and the tuples gave some type-checking on the test data.
                    // So good enough for a unit test.
                    yield return new object[] { t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6 };
                }
            }

            // same dataset as above, but don't want the exception type.
            public static IEnumerable<object[]> GetDataSet1Short()
            {
                foreach (var t in DataSet1)
                {
                    yield return new object[] { t.Item1, t.Item2, t.Item3, t.Item4, t.Item5 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetDataSet1), MemberType=typeof(TestDataGenerator))]
        public void EnsureStorageContextEquivalenceViaStringInits(
            string contextStringIn, bool? setMute, sc contextObjectIn,
            string contextStringOut, sc contextObjectOut,
            Type exceptionTypeExpected)
        {
            sc sc1 = null;
            sc scImplicitMuting = null;

            if (contextStringIn != null)
            {
                bool gotException = false;
                try
                {
                    sc1 = new sc(contextStringIn, setMute);
                    scImplicitMuting = new sc(contextStringIn, null);
                }
                catch (Exception e)
                {
                    e.ShouldBeOfType(exceptionTypeExpected);
                    gotException = true;
                }
                if (exceptionTypeExpected != null)
                {
                    gotException.ShouldBeTrue($"Should have gotten exception for: '{contextStringIn}'");
                }

                if (contextStringOut != null)
                {
                    // confirm result via string.
                    var sc1Str = sc1.ClientRequestID;
                    contextStringOut.ShouldNotBeNullOrWhiteSpace();
                    sc1Str.ShouldNotBeNullOrWhiteSpace();
                    sc1Str.ShouldBe(contextStringOut);
                }

                if (contextObjectOut != null)
                {
                    // confirm result via object.
                    contextObjectOut.ShouldBeEquivalentTo(sc1);
                    AreEquivalent(contextObjectOut, sc1, out string reason).ShouldBeTrue(reason);
                }
            }

            if (contextObjectIn != null)
            {
                sc1 = contextObjectIn;

                if (contextStringOut != null)
                {
                    // confirm result via string.
                    var sc1Str = sc1.ClientRequestID;
                    contextStringOut.ShouldNotBeNull();
                    contextStringOut.ShouldBe(sc1Str);
                }

                if (contextObjectOut != null)
                {
                    // confirm result via object.
                    contextObjectOut.ShouldBeEquivalentTo(sc1);
                    AreEquivalent(contextObjectOut, sc1, out string failedBecause).ShouldBeTrue(failedBecause);
                }
            }
        }

        /// <summary>
        /// Same set of validations, except via CreateSafe, which should not throw an exception
        /// </summary>
        [Theory]
        [MemberData(nameof(TestDataGenerator.GetDataSet1Short), MemberType=typeof(TestDataGenerator))]
        public void EnsureStorageContextEquivalenceViaCreateSafe(
            string contextStringIn, bool? setMute, sc contextObjectIn,
            string contextStringOut, sc contextObjectOut)
        {
            sc sc1 = null;

            if (contextStringIn != null)
            {
                try
                {
                    sc1 = sc.CreateSafe(contextStringIn, setMute);
                }
                catch (Exception e)
                {
                    e.ShouldBeNull($"CreateSafe shouldn't get an exception, got {e.Message}");
                }

                if (contextStringOut != null)
                {
                    // confirm result via string.
                    var sc1Str = sc1.ClientRequestID;
                    contextStringOut.ShouldNotBeNullOrWhiteSpace();
                    sc1Str.ShouldNotBeNullOrWhiteSpace();
                    sc1Str.ShouldBe(contextStringOut);
                }

                if (contextObjectOut != null)
                {
                    // confirm result via object.
                    AreEquivalent(contextObjectOut, sc1, out string reason).ShouldBeTrue(reason);
                }
            }

            if (contextObjectIn != null)
            {
                sc1 = contextObjectIn;

                if (contextStringOut != null)
                {
                    // confirm result via string.
                    var sc1Str = sc1.ClientRequestID;
                    contextStringOut.ShouldNotBeNull();
                    contextStringOut.ShouldBe(sc1Str);
                }

                if (contextObjectOut != null)
                {
                    // confirm result via object.
                    contextObjectOut.ShouldBeEquivalentTo(sc1);
                    AreEquivalent(contextObjectOut, sc1, out string failedBecause).ShouldBeTrue(failedBecause);
                }
            }
        }

        /// <summary>True if two StorageContexts have the same value.</summary>
        private static bool AreEquivalent(sc lhs, sc rhs, out string reason)
        {
            reason = string.Empty;
            if (object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (lhs.ClientRequestID != rhs.ClientRequestID)
            {
                reason = $"ClientRequestID: \"{lhs.ClientRequestID}\" != \"{rhs.ClientRequestID}\"";
                return false;
            }

            var lhsObj = lhs.ClientRequestIdAsJObject;
            var rhsObj = rhs.ClientRequestIdAsJObject;

            if (lhs == null || rhs == null)
            {
                reason = "ClientRequestIdAsJObject: one is null";
                return false;
            }

            if (!JToken.DeepEquals(lhsObj, rhsObj))
            {
                reason = "DeepEquals failed";
                return false;
            }

            if (lhs.IsMuted != rhs.IsMuted)
            {
                reason = $"IsMuted: {lhs.IsMuted} != {rhs.IsMuted}";
                return false;
            }

            if (lhs.TrackingETag != lhs.TrackingETag)
            {
                reason = $"TrackingETag: {lhs.TrackingETag} != {rhs.TrackingETag}";
                return false;
            }

            if (lhs.ETag != rhs.ETag)
            {
                reason = $"ETag: {lhs.ETag} != {rhs.ETag}";
                return false;
            }

            return true;
        }
    }
}

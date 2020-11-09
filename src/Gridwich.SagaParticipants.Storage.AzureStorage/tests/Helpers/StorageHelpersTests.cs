using System;
using System.Collections.Generic;

using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Shouldly;
using Xunit;

// Tuple types used for the different test datasets below
using TupleDS2 = System.Tuple<string, string, string, string, System.Type>;
using TupleDS3 = System.Tuple<string, bool, string, System.Type>;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.Helpers
{
    /// <summary>
    /// Tests for StorageHelpers
    /// </summary>
    public class StorageHelpersTests
    {
        //////////////////////////////////////////////////////////////////////////////////////
        private static partial class TestDataGenerator
        {
            // Can't do these as simple InlineData as they aren't constants
            private static readonly TimeSpan[] DataSet1 = new TimeSpan[]
            {
                new TimeSpan(hours: 10, minutes: 22, seconds: 11),
                new TimeSpan(hours: 0, minutes: 0, seconds: 0),
                new TimeSpan(250L),
                new TimeSpan(days: 1, hours: 11, minutes: 0, seconds: 0, milliseconds: 333),
            };

            public static IEnumerable<object[]> GetDataSet1()
            {
                foreach (var t in DataSet1)
                {
                    yield return new object[] { t };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetDataSet1), MemberType = typeof(TestDataGenerator))]
        public void CreateTimeRangeForURL_CheckSpan(TimeSpan ttl)
        {
            var now = DateTimeOffset.UtcNow;
            TimeRange t = null;

            try
            {
                t = StorageHelpers.CreateTimeRangeForUrl(ttl);
            }
            catch (Exception e)
            {
                e.ShouldBeNull("CreateTimeRangeForUrl should not have thrown an exception");
            }

            // allow this much latitude for equality.  The idea is to accomodate a small
            // difference to account for this test executing DateTimeOffset.UtcNow vs.
            // the function under test doing that.
            var fuzzMS = 5;

            // timespan should be within (say fuzzMs milliseconds of expected)

            var expectedStart = now.AddSeconds(-StorageServiceConstants.SecondsToBackDateForSasUrl);
            var expectedEnd = now.Add(ttl);

            // See if start time falls within +/- fuzzMS milliseconds of expected

            var fuzzyStart = expectedStart.AddMilliseconds(-fuzzMS);
            fuzzyStart.ShouldBeLessThanOrEqualTo(t.StartTime);
            fuzzyStart = expectedStart.AddMilliseconds(fuzzMS);
            fuzzyStart.ShouldBeGreaterThanOrEqualTo(t.StartTime);

            // Same for end as (now + ttl) +/- fuzzMS milliseconds
            var fuzzyEnd = expectedEnd.AddMilliseconds(-fuzzMS);
            fuzzyEnd.ShouldBeLessThanOrEqualTo(t.EndTime);
            fuzzyEnd = expectedEnd.AddMilliseconds(fuzzMS);
            fuzzyEnd.ShouldBeGreaterThanOrEqualTo(t.EndTime);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        private static partial class TestDataGenerator
        {
            private const string DS2p1 = StorageServiceConstants.AzureStorageProtocol;
            private const string DS2p2 = StorageServiceConstants.AzureStorageDnsBlobSuffix;
            private static readonly TupleDS2[] DataSet2 = new TupleDS2[]
            {
                // accountName, containerName, blobName, expectedResult, expectedExceptionType
                new TupleDS2("fred", "con1", "", $@"{DS2p1}://fred.{DS2p2}/con1", null),
                new TupleDS2("fred", "con1", "someBlob.txt", $@"{DS2p1}://fred.{DS2p2}/con1/someBlob.txt", null),
                new TupleDS2("badUpperCase", "con1", "someBlob.txt", null, typeof(ArgumentException)),
                // try each arg in turn as null
                new TupleDS2(null, "con1", "someBlob.txt", null, typeof(ArgumentNullException)),
                new TupleDS2("good", null, "someBlob.txt", null, typeof(ArgumentNullException)),
                new TupleDS2("good", "con222", null, null, typeof(ArgumentNullException)),
            };
            public static IEnumerable<object[]> GetDataSet2()
            {
                foreach (var t in DataSet2)
                {
                    // An admittedly inelegant way to return the data, but Xunit insists on
                    // object[] and the tuples gave some type-checking on the test data.
                    // So good enough for a unit test.
                    yield return new object[] { t.Item1, t.Item2, t.Item3, t.Item4, t.Item5 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetDataSet2), MemberType = typeof(TestDataGenerator))]
        public void BuildBlobStorageUri_Test(
                string accountName, string containerName, string blobName,
                string expectedResult, Type expectedExceptionType)
        {
            Uri res;
            try
            {
                res = StorageHelpers.BuildBlobStorageUri(accountName, containerName, blobName);
            }
            catch (Exception e)
            {
                expectedExceptionType.ShouldNotBeNull();
                e.ShouldBeOfType(expectedExceptionType);
                return;
            }

            expectedExceptionType.ShouldBeNull();
            res.ToString().ShouldBe(expectedResult);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        private static partial class TestDataGenerator
        {
            private const string DS3p1 = StorageServiceConstants.AzureStorageProtocol;
            private const string DS3p2 = StorageServiceConstants.AzureStorageDnsSuffix;
            private const string DS3p3 = StorageServiceConstants.AzureStorageDnsBlobSuffix;
            private static readonly TupleDS3[] DataSet3 = new TupleDS3[]
            {
                // accountName, buildForBlobService, expectedResult, expectedExceptionType
                new TupleDS3("fred", false, $@"{DS3p1}://fred.{DS3p2}/", null),
                new TupleDS3("fred", true, $@"{DS3p1}://fred.{DS3p3}/", null),
                new TupleDS3("sally22", false, $@"{DS3p1}://sally22.{DS3p2}/", null),
                new TupleDS3("sally22", true, $@"{DS3p1}://sally22.{DS3p3}/", null),
                new TupleDS3("   sally22 ", false, null, typeof(ArgumentException)),
                new TupleDS3("   sally22 ", true, null, typeof(ArgumentException)),
                new TupleDS3("badUpperCase", false, null, typeof(ArgumentException)),
                new TupleDS3("badUpperCase", true, null, typeof(ArgumentException)),

                // try different flavors of empty
                new TupleDS3(null, false, null, typeof(ArgumentNullException)),
                new TupleDS3(null, true, null, typeof(ArgumentNullException)),
                new TupleDS3("", false, null, typeof(ArgumentNullException)),
                new TupleDS3("", true, null, typeof(ArgumentNullException)),
                new TupleDS3("   ", false, null, typeof(ArgumentNullException)),
                new TupleDS3("   ", true, null, typeof(ArgumentNullException)),
            };
            public static IEnumerable<object[]> GetDataSet3()
            {
                foreach (var t in DataSet3)
                {
                    // An admittedly inelegant way to return the data, but Xunit insists on
                    // object[] and the tuples gave some type-checking on the test data.
                    // So good enough for a unit test.
                    yield return new object[] { t.Item1, t.Item2, t.Item3, t.Item4 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetDataSet3), MemberType = typeof(TestDataGenerator))]
        public void BuildStorageAccountUri_Test(string accountName, bool buildForBlobService, string expectedResult, Type expectedExceptionType)
        {
            Uri res;
            try
            {
                res = StorageHelpers.BuildStorageAccountUri(accountName, buildForBlobService);
            }
            catch (Exception e)
            {
                expectedExceptionType.ShouldNotBeNull();
                e.ShouldBeOfType(expectedExceptionType);
                return;
            }

            expectedExceptionType.ShouldBeNull();
            res.ToString().ShouldBe(expectedResult);
        }

        [Theory(DisplayName = "Ensure that Storage URI escaping to something suitable for SAS computation works")]
        [InlineData(@"https://xyz.blob.core.windows.net/ac/fred.txt",
                    @"https://xyz.blob.core.windows.net/ac/fred.txt", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/a/b/c/d/efg/fred.txt",
                    @"https://xyz.blob.core.windows.net/a/b/c/d/efg/fred.txt", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/ac/fr d.txt",
                    @"https://xyz.blob.core.windows.net/ac/fr%20d.txt", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/a%2Fb/c/d/efg%2ffred.txt",
                    @"https://xyz.blob.core.windows.net/a/b/c/d/efg/fred.txt", null)]

        // Container only, convention is trailing slash in this case.
        // Strictly speaking, spaces aren't allowed in container names anyway,
        // but we're not vetting beyond escapes here anyway.
        [InlineData(@"https://xyz.blob.core.windows.net/a%20c",
                    @"https://xyz.blob.core.windows.net/a%20c", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/a c/",
                    @"https://xyz.blob.core.windows.net/a%20c", null)]

        // Container, with Query String
        [InlineData(@"https://xyz.blob.core.windows.net/abc",
                    @"https://xyz.blob.core.windows.net/abc", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/abc?abC=%3DJJ&a=%3fb",
                    @"https://xyz.blob.core.windows.net/abc?abC=%3DJJ&a=%3fb", null)]

        // Host only, with and w/o query string
        // Note that this case never arises in real Azure Storage use.  For cases
        // where the storage account is the subject resource (e.g. 'xyz' below),
        // the URI used actually points to management.azure.com, not to a storage URI.
        // See: https://docs.microsoft.com/en-us/rest/api/storagerp/storage-sample-create-account for an example.
        [InlineData(@"https://xyz.blob.core.windows.net?abC=%3DJJ&a=%3fb",
                    @"https://xyz.blob.core.windows.net/?abC=%3DJJ&a=%3fb", null)]
        [InlineData(@"https://xyz.blob.core.windows.net/?abC=%3DJJ&a=%3fb",
                    @"https://xyz.blob.core.windows.net/?abC=%3DJJ&a=%3fb", null)]
        // Check for trailing slash on "just host"
        [InlineData(@"https://abc.com",
                    @"https://abc.com/", null)]
        [InlineData(@"https://abc.com/",
                    @"https://abc.com/", null)]

        // Check that the '+' is not mapped over as space.
        [InlineData(@"https://xyz.blob.core.windows.net/a%20c/d+e",
                    @"https://xyz.blob.core.windows.net/a%20c/d+e", null)]
        // An odd case, ensure that an encoded plus ends up as a plus sign (i.e., not unencoded, swapped to space, reencoded.)
        [InlineData(@"https://xyz.blob.core.windows.net/a%2Bc/",
                    @"https://xyz.blob.core.windows.net/a+c", null)]

        // Ensure query string stays untouched.

        // Unencoded blanks
        [InlineData(@"https://xyz.blob.core.windows.net/a c?abC=%3DJJ&a=%3fb",
                    @"https://xyz.blob.core.windows.net/a%20c?abC=%3DJJ&a=%3fb", null)]
        // Encoded slashes
        [InlineData(@"https://xyz.blob.core.windows.net/ac%2FABfred.txt?xyz=abc%26def=12&x=ab%3Dcd",
                    @"https://xyz.blob.core.windows.net/ac/ABfred.txt?xyz=abc%26def=12&x=ab%3Dcd", null)]
        // Unescaped unicode DBCS character
        [InlineData("https://xyz.blob.core.windows.net/ac/fr\u4EDDed.txt?xyz=abc%26def=12&x=ab%3Dcd",
                    @"https://xyz.blob.core.windows.net/ac/fr%E4%BB%9Ded.txt?xyz=abc%26def=12&x=ab%3Dcd", null)]
        // Already-escaped unicode DBCS character
        [InlineData("https://xyz.blob.core.windows.net/ac/fr%E4%BB%9Ded.txt?xyz=abc%26def=12&x=ab%3Dcd",
                    @"https://xyz.blob.core.windows.net/ac/fr%E4%BB%9Ded.txt?xyz=abc%26def=12&x=ab%3Dcd", null)]

        // And some bad ones
        // ... no host
        [InlineData("https://", null, typeof(UriFormatException))]
        // ... can't use encoding for '/' until after host
        [InlineData(@"https://xyz.blob.core.windows.net%2Fa%20c?abC=%3DJJ&a=%3fb", null, typeof(UriFormatException))]
        // %2F too far forward in URL
        [InlineData(@"https:/%2Fxyz.blob.core.windows.net/ac%2Ffred.txt", null, typeof(UriFormatException))]
        // Bad protocol (missing ':').
        [InlineData(@"https//", null, typeof(UriFormatException))]
        // Unknown protocol -- actually okay as we don't force https.  Normal Uri class behavior.
        [InlineData(@"XXhttps://", @"xxhttps:///", null)]
        // Nothing (well, this one really blows up trying to pack "" into the test Uri before calling the StorageHelpers helper method)
        [InlineData("", null, typeof(UriFormatException))]

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:URI parameters should not be strings", Justification = "We should disable this warning for tests")]
        public void TestUriUnescape(string inputUrl, string expectedUrl, Type exceptionType)
        {
            bool gotException = false;
            Uri outUri = null;

            Uri ret;
            try
            {
                Uri inUri = new Uri(inputUrl);

                if (!string.IsNullOrWhiteSpace(expectedUrl))
                {
                    outUri = new Uri(expectedUrl);
                }

                ret = inUri.NormalizeUriEscaping();
            }
            catch (Exception e)
            {
                exceptionType.ShouldNotBeNull($"Received unexpected exception: {e.Message}");
                e.ShouldBeOfType(exceptionType, "Unexpected exception type");
                return;
            }

            ret.AbsoluteUri.ShouldBe(expectedUrl);
            ret.AbsoluteUri.ShouldBe(outUri?.OriginalString);
            gotException.ShouldBe(exceptionType != null, "Didn't get expected exception");
        }
    }
}
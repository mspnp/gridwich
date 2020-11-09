using System;
using System.Text.RegularExpressions;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Newtonsoft.Json;

using Shouldly;

using Xunit;

namespace Gridwich.CoreTests.Helpers
{
    public class JSONHelperTests
    {
        public JSONHelperTests()
        {
            // ensure that our options are set up as the default for Serializers.
            JsonHelpers.SetupJsonSerialization();
        }

        [Fact(DisplayName = "JSON Serialization Options")]
        // Test that default JSON serialization is indeed camelCase
        // Ignore pretty-print formatting -- test will strip out spaces so will work with either be Indented or None.
        public void JSON_Serialization_Casing()
        {
            // Serialize T1aStruct & ensure Json matches T1aJson
            string t1aSerialized = JsonConvert.SerializeObject(T1aStruct);

            t1aSerialized = Regex.Replace(t1aSerialized, @"\s", string.Empty); // get rid of whitespace (to make formatting Indented & None equivalent)

            // DebugHelpers.WriteLine("LHS = '{0}'\nRHS = '{1}'", t1aSerialized, T1aJson);

            (t1aSerialized == T1aJson).ShouldBeTrue();
        }

        [Fact(DisplayName = "JSON Serialization Serialize 2")]
        // Test that default JSON serialization is indeed camelCase for Gridwich models.
        // Ignore pretty-print formatting -- test will strip out spaces so will work with either be Indented or None.
        public void JSON_Serialization_Casing2()
        {
            RequestBlobAnalysisCreateDTO src = new RequestBlobAnalysisCreateDTO() { BlobUri = new Uri("http://xyz.com/") };
            string srcJsonExpected = $"{{'blobUri':'{src.BlobUri}'}}".Replace('\'', '"');

            var srcJsonActual = JsonConvert.SerializeObject(src);

            srcJsonActual = Regex.Replace(srcJsonActual, @"\s", string.Empty); // get rid of whitespace (to make formatting Indented & None equivalent)

            // Console.WriteLine("Expected = '{0}'\nActual   = '{1}'", srcJsonExpected, srcJsonActual);

            (srcJsonExpected == srcJsonActual).ShouldBeTrue();
        }

        [Fact(DisplayName = "JSON Deserialization Options 1")]
        // Test that default JSON deserialization does map camelCase correctly into a non-camelCase struct.
        public void JSON_Deserialization_Escaping()
        {
            var tester = new T1()
            {
                FirstName = "Fred",
                MiddleName = "Jim",
                Surname = "Smith",
                TheEnum = T1.X.B,
                Tier = BlobAccessTier.Cool,
            };
            string s = JsonHelpers.SerializeObjectToString(tester);
            string s1 = JsonConvert.SerializeObject(tester);
            JsonHelpers.JsonEqual(s1, s).ShouldBeTrue();
        }

        [Fact(DisplayName = "JSON Equivalence")]
        // Test check that JSON objects are equivalent
        public void JSON_Equivalence_01X()
        {
            string o1 = @"
            {
                 'String': 'A string' ,
                 'Integer': 12345 ,
                 'Items' : [ 1, 2 ]
            }".Replace("'", "\"");

            // Note different member order
            string o2 = @"
            {
                'Integer': 12345,
                'Items' : [ 1, 2 ],
                'String': 'A string'
            }".Replace("'", "\"");

            // Note extra member
            string o3 = @"
            {
                'Integer': 12345,
                'Items' : [ 1, 2 ],
                'String': 'A string',
                'Extra' : 33
            }".Replace("'", "\"");

            JsonHelpers.JsonEqual(o1, o2).ShouldBeTrue();
            JsonHelpers.JsonEqual(o1, o1).ShouldBeTrue();
            JsonHelpers.JsonEqual(o2, o2).ShouldBeTrue();
            JsonHelpers.JsonEqual(o3, o3).ShouldBeTrue();

            JsonHelpers.JsonEqual(o2, o3).ShouldBeFalse();
            JsonHelpers.JsonEqual(o1, o3).ShouldBeFalse();
        }

        [Fact(DisplayName = "JSON Compression")]
        // Test JSON compression
        public void JSON_Compression_01()
        {
            string json1 = @"
            {
                'abc'   :'d e f' ,
                'mouse' : [
                    'a': 1 ,
                    'b': 'cd\ne'
                    ],
                'xyz':2 , 'mno' :33
            }".Replace("'", "\"");
            _ = JsonHelpers.CompressJson(json1);

            // DebugHelpers.WriteLine("Compressed #1 = '{0}'", res);
            true.ShouldBeTrue();
        }

        [Fact(DisplayName = "JSON Deserialization Options 2")]
        // Test that default JSON deserialization does map camelCase correctly into a non-camelCase struct.
        public void JSON_Deserialization_Casing()
        {
            // Deserialize T1aJson & ensure all of the struct fields get populated as per T1aStruct.

            // start the target struct with "bad" values
            const string SomeName = "<someName>";
            var theEnumValue = T1.X.C;
            var theTierValue = BlobAccessTier.Hot;

            T1 t2aResult = new T1
            {
                FirstName = null,
                MiddleName = SomeName,
                Surname = null,
                TheEnum = theEnumValue,
                Tier = theTierValue,
            };

            // Equivalent of t2aResult - whitespace "compressed" version of the JSON input/output.
            string t2aJson = $"{{'middleName':'{SomeName}','theEnum':'{theEnumValue}','tier':'{theTierValue}'}}".Replace('\'', '"');

            var t2aResultActual = JsonConvert.DeserializeObject<T1>(t2aJson);

            bool gotExpectedResult = T1Equals(t2aResult, t2aResultActual);

            if (!gotExpectedResult)
            {
               // DebugHelpers.WriteLine("Bad Deserialization: Expected {0}, Got {1}", ToString(t2aResult), ToString(t2aResultActual));
            }

            gotExpectedResult.ShouldBeTrue();
        }

        // Test whether JObject parsing is retaining case sensitive keys.
        // Note that JSONConvert now forces Camel Casing, but we have lots of JObject.Parse(...) in the code that
        // is using JObject.Keys.Contains(...) against non-Camel property names to determine for success.

        [Fact(DisplayName = "Deserialization and Property casing lookup")]
        public void JSON_Deserialize_And_Lookup()
        {
            // Note: following presumes JObject parsing...
            string mixJson = "{ 'abc': 1, 'ABC': 2, 'AbC': 3 }".Replace('\'', '"');

            JsonHelpers.ContainsProperty(mixJson, "AbC", true).ShouldBeTrue();
            JsonHelpers.ContainsProperty(mixJson, "ABC", true).ShouldBeTrue();
            JsonHelpers.ContainsProperty(mixJson, "abc", true).ShouldBeTrue();

            JsonHelpers.ContainsProperty(mixJson, "AbC").ShouldBeTrue();
            JsonHelpers.ContainsProperty(mixJson, "ABC").ShouldBeTrue();
            JsonHelpers.ContainsProperty(mixJson, "abc").ShouldBeTrue();

            JsonHelpers.ContainsProperty(mixJson, "abc", false).ShouldBeTrue();
            // bad casing tests
            JsonHelpers.ContainsProperty(mixJson, "aBc", false).ShouldBeTrue();
            JsonHelpers.ContainsProperty(mixJson, "aBc", true).ShouldBeFalse();
            // No such property
            JsonHelpers.ContainsProperty(mixJson, "XXX", true).ShouldBeFalse();
            JsonHelpers.ContainsProperty(mixJson, "XXX", false).ShouldBeFalse();
            // Bad JSON
            bool workedAsExpected = false;
            try
            {
                JsonHelpers.ContainsProperty("xcysdf", "xxx", false).ShouldBeFalse();
                workedAsExpected = true;
            }
            catch (JsonException)
            {
                workedAsExpected = true;
            }
            catch (Exception)
            {
                // nothing to do, threw an exception, but not right type, so fail.
            }
            workedAsExpected.ShouldBeTrue();
        }

        // A simple test class
        private struct T1
        {
            public string FirstName;
            public string MiddleName;
            public string Surname;
            public enum X { A, B, C }
            public X TheEnum;

            public BlobAccessTier Tier;
        }

        // An exemplar for [de]serialize.  Note variety in casing of member names.
        private static readonly T1 T1aStruct = new T1
            {
                FirstName = "fiRst",
                MiddleName = "middlE",
                Surname = "Surname",
                TheEnum = T1.X.B,
                Tier = BlobAccessTier.Cool
            };
        // Equivalent of T1a - whitespace "compressed" version of the JSON input/output.
        private static readonly string T1aJson =
            ($"{{'firstName':'{T1aStruct.FirstName}','middleName':'{T1aStruct.MiddleName}'" +
            $",'surname':'{T1aStruct.Surname}','theEnum':'B','tier':'Cool'}}")
            .Replace('\'', '"');

        // Check T1 struct equality here (keep struct w/o any methods)
        private static bool T1Equals(T1 lhs, T1 rhs)
        {
            bool areEqual = (lhs.FirstName == rhs.FirstName)
                        && (lhs.MiddleName == rhs.MiddleName)
                        && (lhs.Surname == rhs.Surname)
                        && (lhs.TheEnum == rhs.TheEnum)
                        && lhs.Tier.Equals(rhs.Tier);
            return areEqual;
        }
    }
}

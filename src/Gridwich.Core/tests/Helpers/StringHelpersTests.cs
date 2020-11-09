using System;
using Gridwich.Core.Helpers;
using Shouldly;
using Xunit;

namespace Gridwich.CoreTests.Helpers
{
    public class StringHelpersTests
    {
        [Theory(DisplayName="Test StringHelpers.NullIfNullOrWhiteSpace")]
        [InlineData("abcdef", "abcdef")]
        [InlineData("abc\ndef", "abc\ndef")]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("     ", null)]
        [InlineData("   A    ", "   A    ")]
        [InlineData("  \n  ", null)]
        [InlineData("  \t  ", null)]
        [InlineData("\n", null)]
        [InlineData("\t", null)]
        public void TestNullIfNullOrWhiteSpace(string input, string expectedOutput)
        {
            string result = StringHelpers.NullIfNullOrWhiteSpace(input);

            result.ShouldBe(expectedOutput);
        }

        [Theory(DisplayName="Test StringHelpers.NullIfNullOrEmpty")]
        [InlineData("abcdef", "abcdef")]
        [InlineData("abc\ndef", "abc\ndef")]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("     ", "     ")]
        [InlineData("   A    ", "   A    ")]
        [InlineData("  \n  ", "  \n  ")]
        [InlineData("  \t  ", "  \t  ")]
        [InlineData("\n", "\n")]
        [InlineData("\t", "\t")]
        public void TestNullIfNullOrEmpty(string input, string expectedOutput)
        {
            string result = StringHelpers.NullIfNullOrEmpty(input);

            result.ShouldBe(expectedOutput);
        }

        [Theory(DisplayName="Test StringHelpers.Camelize")]
        [InlineData("ABCdef", "aBCdef")]
        [InlineData("ABCD", "aBCD")]
        [InlineData("Abcd", "abcd")]
        [InlineData("ABC DEf", "aBC DEf")]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("  \n  ", "  \n  ")]
        [InlineData("  \t  ", "  \t  ")]
        public void TestCamelize(string input, string expectedOutput)
        {
            string result = StringHelpers.Camelize(input);

            expectedOutput.ShouldBe(result);
        }

        [Theory(DisplayName="Test StringHelpers.Format")]
        [InlineData("S={1}, T={0}", new object[] { 22, 33 }, "S=33, T=22")]
        [InlineData("", new object[] { 22, 44 }, "")]
        [InlineData("ABCD EFGH", new object[] { }, "ABCD EFGH")]
        [InlineData("S={1}, T={0}", new object[] { 22, 33, 44, 55 }, "S=33, T=22")]
        [InlineData("ABC DEf {{}}", new object[] { },  "ABC DEf {}")]
        [InlineData(null, new object[] { 22 }, null, typeof(ArgumentNullException))]
        public void TestFormat(string format, object[] args, string expectedOutput, Type exceptionType = null)
        {
            string result = null;

            try
            {
                result = StringHelpers.Format(format, args);
            }
            catch (Exception e)
            {
                if (exceptionType != null)
                {
                    // we expect an exception with a failure...
                    e.ShouldBeOfType(exceptionType);
                }
                else
                {
                    // should have worked, but failed with exception
                    e.ShouldBeNull("Format threw exception, but shouldn't have");
                }
            }

            result.ShouldBe(expectedOutput);
        }
    }
}

using System;

using Gridwich.Core.Helpers;
using Shouldly;
using Xunit;


namespace Gridwich.CoreTests
{
    /// <summary>
    /// Tests related to the TestHelper class
    /// </summary>
    public class TestHelpersTests
    {
        [Theory]
        [InlineData(null,
                    "/home/vsts/work/1/s/src/Gridwich.Host.FunctionAppTests/bin/Release/netcoreapp3.1",
                    "src", '/',
                    "Gridwich.Host.FunctionApp/sx.json",
                    "/home/vsts/work/1/s/src/Gridwich.Host.FunctionApp/sx.json")]
        [InlineData(null,
                    "C:/users/fred/home/vsts/work/1/s/src/Gridwich.Host.FunctionAppTests/bin/Release/netcoreapp3.1/bin",
                    "src", '\\',
                    "Gridwich.Host.FunctionApp/sx.json",
                    @"C:\users\fred\home\vsts\work\1\s\src\Gridwich.Host.FunctionApp\sx.json")]
        [InlineData(null,
                    "C:/users/fred/home/vsts/work/1/s/src/Gridwich.Host.FunctionAppTests/bin/Release/netcoreapp3.1/bin",
                    "src", '/',
                    "Gridwich.Host.FunctionApp/sx.json",
                    @"C:/users/fred/home/vsts/work/1/s/src/Gridwich.Host.FunctionApp/sx.json")]
        [InlineData(null,
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src",
                    "src", '/',
                    "Gridwich.Host.FunctionApp/sx.json",
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src/Gridwich.Host.FunctionApp/sx.json")]
        [InlineData(null,
                    "/c/Users/fred/Documents/./Source/Github/Brxdds/src",
                    "src", '/',
                    "Gridwich.Host.FunctionApp/./sx.json",
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src/Gridwich.Host.FunctionApp/sx.json")]
        [InlineData(null,
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src",
                    "src", '\\',
                    "Gridwich.Host.FunctionApp/sx.json",
                    @"\c\Users\fred\Documents\Source\Github\Brxdds\src\Gridwich.Host.FunctionApp\sx.json")]
        [InlineData(null,
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src/", // trailing slash on path
                    "src", '\\',
                    "Gridwich.Host.FunctionApp/sx.json",
                    @"\c\Users\fred\Documents\Source\Github\Brxdds\src\Gridwich.Host.FunctionApp\sx.json")]
        [InlineData(null,
                    "/c/Users/fred/Documents/Source/Github/Brxdds/src/", // trailing slash on path
                    "src", '\\',
                    "./Gridwich.Host.FunctionApp/sx.json",
                    @"\c\Users\fred\Documents\Source\Github\Brxdds\src\Gridwich.Host.FunctionApp\sx.json")]
        [InlineData(typeof(System.ArgumentOutOfRangeException), // too many '..'s
                    "/c/Users/fred/Documents/xyz/abc",
                    "xyz", '/',
                    "../../../../../../../mouse/sx.json",
                    null)]
        [InlineData(typeof(System.ArgumentOutOfRangeException), // element not in currentDirectory path
                    "/c/Users/fred/Documents/xyz/abc",
                    "missing", '/',
                    "../mouse/sx.json",
                    null)]
        /// <summary>Check that the path climbing is working correctly.</summary>
        public void TestHelperPathTraversal(
                System.Type exceptionTypeExpectedOrNull,
                string currentDirectory,
                string segmentToTrimUpTo,
                char outputSeparator,
                string filenameToPosition,
                string expectedResult)
        {
            string res = string.Empty;
            try
            {
                res = TestHelpers.GetPathRelativeTo(segmentToTrimUpTo, filenameToPosition, currentDirectory, outputSeparator);
            }
            catch (Exception e)
            {
                if (exceptionTypeExpectedOrNull != null)
                {
                    e.ShouldBeOfType(exceptionTypeExpectedOrNull);
                }
                else
                {
                    e.ShouldBeNull("Got unexpected exception");
                }
                return;
            }

            res.ShouldBe(expectedResult);
        }
    }
}

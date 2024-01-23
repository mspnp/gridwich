using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using Xunit;

namespace Gridwich.CoreTests
{
    public class GridwichExceptionTests
    {
        [Fact]
        public void SafeAddToData_Should_ReturnTrue()
        {
            var e = new GridwichTestException();

            var result = e.SafeAddToData("TestKey", "TestValue");

            result.ShouldBeTrue();
            e.Data["TestKey"].ShouldBe("TestValue");
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("TestKey", null)]
        [InlineData(null, "TestValue")]
        [InlineData("", "TestValue")]
        public void SafeAddToData_Should_ReturnFalse(string key, string value)
        {
            var e = new GridwichTestException();

            var result = e.SafeAddToData(key, value);

            result.ShouldBeFalse();
        }

        [Fact]
        public void SafeAddToData_Should_ReturnFalse_WhenKeyAlreadyPresent()
        {
            var e = new GridwichTestException();

            var result1 = e.SafeAddToData("TestKey", "TestValue");
            var result2 = e.SafeAddToData("TestKey", "TestValue");

            result1.ShouldBeTrue();
            result2.ShouldBeFalse();
        }

        [Fact]
        public void AddOperationContext_Should_SetOperationContext()
        {
            var e = new GridwichTestException();
            var context = new JObject();

            e.AddOperationContext(context);

            e.OperationContext.ShouldBe(context);
        }

        [Fact]
        public void AddOperationContext_ShouldNot_SetOperationContext_WhenAlreadyPresent()
        {
            var e = new GridwichTestException();
            var context1 = new JObject();
            var context2 = new JObject();

            e.AddOperationContext(context1);
            e.AddOperationContext(context2);

            e.OperationContext.ShouldBe(context1);
        }

        [Fact]
        public void ToGridwichFailureDTO_Should_ThrowArgumentException()
        {
            var e = new GridwichTestException();

            Assert.Throws<ArgumentException>(() => e.ToGridwichFailureDTO(null, GetType(), null));
            Assert.Throws<ArgumentException>(() => e.ToGridwichFailureDTO(string.Empty, GetType(), null));
            Assert.Throws<ArgumentNullException>(() => e.ToGridwichFailureDTO("HandlerId", null, null));
        }

        [Fact]
        public void ToGridwichFailureDTO_ShouldNot_Fail()
        {
            var e = new GridwichTestException();

            e.ToGridwichFailureDTO("HandlerId", GetType(), null).ShouldNotBeNull();
        }

        private class GridwichTestException : GridwichException
        {
            // Note base could take any LogEventId here
            public GridwichTestException()
                : base("Test", LogEventIds.GridwichUnhandledException, new JObject())
            {
            }
        }
    }
}
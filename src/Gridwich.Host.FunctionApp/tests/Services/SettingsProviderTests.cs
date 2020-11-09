using System;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Host.FunctionApp.Services;
using Gridwich.Host.FunctionAppTests.Services.Utils;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.Host.FunctionAppTests.Services
{
    [ExcludeFromCodeCoverage]
    public class SettingsProviderTests
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsProvider _settingsProvider;

        public SettingsProviderTests()
        {
            _configuration = Mock.Of<IConfiguration>();
            _settingsProvider = new SettingsProvider(_configuration);
        }

        [Fact]
        public void GetAppSettingsValue_ShouldReturnKey_WhenCalled()
        {
            // Arrange
            var settingKey = "Key";
            var expectedValue = "Value";

            // Arrange Mocks
            Mock.Get(_configuration)
                .Setup(x => x.GetSection(settingKey))
                .Returns(new TestConfigurationSection(expectedValue));

            // Act
            var result = _settingsProvider.GetAppSettingsValue(settingKey);

            // Assert
            result.ShouldBe(expectedValue);
        }

        [Fact]
        public void GetAppSettingsValue_ShouldThrow_WhenUriIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _settingsProvider.GetAppSettingsValue(null));
        }
    }
}
using System;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Moq;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloudTests
{
    public class TelestreamCloudClientProviderTests
    {
        private readonly IObjectLogger<TelestreamCloudClientProvider> log = Mock.Of<IObjectLogger<TelestreamCloudClientProvider>>();

        [Fact]
        public void ClientProvider_ShouldNotThrowWithApiKey()
        {
            // Arrange
            // Arrange Mocks
            var settings = Mock.Of<ISettingsProvider>(x => x.GetAppSettingsValue(It.IsAny<string>()) == "string");

            // Act
            var telestreamCloudClientProvider = new TelestreamCloudClientProvider(settings, log);
        }

        [Fact]
        public void ClientProvider_ShouldThrowWithEmptyApiKey()
        {
            // Arrange
            // Arrange Mocks
            var settings = Mock.Of<ISettingsProvider>(x => x.GetAppSettingsValue(It.IsAny<string>()) == string.Empty);

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new TelestreamCloudClientProvider(settings, log));
        }
    }
}

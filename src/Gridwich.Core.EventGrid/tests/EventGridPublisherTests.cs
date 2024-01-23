using Gridwich.Core.Constants;
using Gridwich.Core.EventGrid;
using Gridwich.Core.Interfaces;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Rest.Azure;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.Core.EventGridTests
{
    public class EventGridPublisherTests
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IObjectLogger<EventGridPublisher> _logger;
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly IEventGridClientProvider _eventGridClientProvider;

        public EventGridPublisherTests()
        {
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<EventGridPublisher>>();
            _eventGridClientProvider = Mock.Of<IEventGridClientProvider>();
            _eventGridPublisher = new EventGridPublisher(_logger, _settingsProvider, _eventGridClientProvider);
        }

        [Fact]
        public async Task PublishEventToTopic_ShouldReturnFalse_WhenBadTopicKey()
        {
            // Arrange
            var gridEvent = new EventGridEvent();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundKeySettingName))
                .Returns((string)null);

            // Act
            var result = await _eventGridPublisher.PublishEventToTopic(gridEvent).ConfigureAwait(true);

            // Assert
            result.ShouldBe(false);
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.ExceptionPublishingEvent,
                    It.IsAny<ArgumentException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the handler fails publishing the event");
        }

        [Fact]
        public async Task PublishEventToTopic_ShouldReturnFalse_WhenBadTopicEndpoint()
        {
            // Arrange
            var gridEvent = new EventGridEvent();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundKeySettingName))
                .Returns("TEST");
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(string.Empty);

            // Act
            var result = await _eventGridPublisher.PublishEventToTopic(gridEvent).ConfigureAwait(true);

            // Assert
            result.ShouldBe(false);
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.ExceptionPublishingEvent,
                    It.IsAny<ArgumentException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the handler fails publishing the event");
        }

        [Fact]
        public async Task PublishEventToTopic_ShouldPublish_WhenAllOk()
        {
            // Arrange
            var gridEvent = new EventGridEvent();
            string topicKey = "TOPIC_KEY";
            Uri topicEndpoint = new Uri("http://www.test.com");
            var eventGridClient = Mock.Of<IEventGridClient>();
            var expectedAsyncReturn = new AzureOperationResponse();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundKeySettingName))
                .Returns(topicKey);
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpoint.ToString());
            Mock.Get(eventGridClient)
                .Setup(x => x.PublishEventsWithHttpMessagesAsync(
                    topicEndpoint.Host,
                    It.IsAny<IList<EventGridEvent>>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAsyncReturn);
            Mock.Get(_eventGridClientProvider)
                .Setup(x => x.GetEventGridClientForTopic(Publishing.TopicOutbound, topicKey))
                .Returns(eventGridClient);

            // Act
            var result = await _eventGridPublisher.PublishEventToTopic(gridEvent).ConfigureAwait(true);

            // Assert
            result.ShouldBe(true);
            Mock.Get(eventGridClient).Verify(x =>
                x.PublishEventsWithHttpMessagesAsync(
                    It.IsAny<string>(),
                    It.IsAny<IList<EventGridEvent>>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()), Times.Once);

            // Test Cleanup
            expectedAsyncReturn.Dispose();
        }
    }
}

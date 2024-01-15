using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class BlobDeleteHandlerTests
    {
        #region Boilerplate
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly BlobDeleteHandler _handler;
        private readonly IObjectLogger<BlobDeleteHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;
        public BlobDeleteHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobDeleteHandler>>();
            _handler = new BlobDeleteHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }
        #endregion /* Boilerplate */

        /// <summary>Check the handler ID matches</summary>
        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange:
            string expectedHandlerId = "13244CB5-64F7-4C64-87B7-D7B0001E028F";
            // Act:
            var actualHandlerId = _handler.GetHandlerId();
            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId, StringCompareShould.IgnoreCase);
        }

        /// <summary>
        /// We should handle any version of our event
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="dataVersion">The data version.</param>
        [Theory]
        [InlineData(CustomEventTypes.RequestBlobDelete, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobDelete, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobDelete, "NotAnInt")]
        public void HandlesEvent_ShouldHandle_EventType_and_Version(string eventType, string dataVersion)
        {
            // Arrange:
            //    - See InlineData
            // Act:
            bool eventHandled = _handler.HandlesEvent(eventType, dataVersion);
            // Assert:
            eventHandled.ShouldBeTrue();
        }

        /// <summary>
        /// Check that we don't handle the event of others
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="dataVersion">The data version.</param>
        [Theory]
        [InlineData(CustomEventTypes.RequestBlobAnalysisCreate, "1.0")]
        [InlineData(EventTypes.StorageBlobDeletedEvent, "1.0")]
        [InlineData("NotAnExpectedEventType", "1.0")]
        [InlineData("NotAnExpectedEventType", "NotAnInt")]
        public void HandlesEvent_ShouldNotHandle_EventType_and_Version(string eventType, string dataVersion)
        {
            // Arrange:
            //    - See InlineData
            // Act:
            bool eventHandled = _handler.HandlesEvent(eventType, dataVersion);
            // Assert:
            eventHandled.ShouldBeFalse();
        }

        /// <summary>Verify that the happy path doesn't log and returns the right event</summary>
        [Fact]
        public async Task DoWorkAsync_ShouldReturnTrueAndNotLog_WhenNoErrors()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var opCtx = JsonHelpers.JsonToJObject("{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}");
            var opCtxString = JsonHelpers.SerializeOperationContext(opCtx);

            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobDelete,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobDeleteDTO
                {
                    BlobUri = new Uri(_expectedInboxUrl),
                    OperationContext = opCtx,
                }),
            };
            var expectedBlobMetadata = JsonHelpers.JsonToJObject("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}", true);

            var ctx = StorageClientProviderContext.None;

            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(expectedBlobMetadata);
            Mock.Get(_storageService)
                .Setup(x => x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act:
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert:
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.InvalidUriInBlobDeleteHandler, It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds [2]");
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "No exceptions should be logged when the publishing succeeds [3]");
            Mock.Get(_logger).Verify(x =>
                x.LogEvent(LogEventIds.NoSuchBlobInBlobDeleteHandler, It.IsAny<string>(), It.IsAny<Uri>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds [4]");

            Mock.Get(_storageService).Verify(x =>
                x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_storageService).Verify(x =>
                x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to delete the blob.");
            Mock.Get(_eventGridPublisher)
                .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Exactly(2),
                "Should only publish ACK + Scheduled.");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobDeleteScheduled);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobDeleteScheduledDTO));
            var data = (ResponseBlobDeleteScheduledDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
            data.BlobUri.ToString().ShouldBe(_expectedInboxUrl);
            data.BlobMetadata.ShouldNotBeNull();
            data.BlobMetadata.ContainsKey("someProp1").ShouldBeTrue();
            data.BlobMetadata["someProp1"].ShouldBe("someValue1");
            data.BlobMetadata.ContainsKey("someProp2").ShouldBeTrue();
            data.BlobMetadata["someProp2"].ShouldBe("someValue2");
        }

        /// <summary>Verify that we get a failure when the named blob isn't in storage</summary>
        [Fact]
        public async Task DoWorkAsync_ShouldReturnAFailureEventandLog_WhenMissingBlob()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var opCtx = JsonHelpers.JsonToJObject("{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}");

            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobDelete,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobDeleteDTO
                {
                    BlobUri = new Uri(_expectedInboxUrl),
                    OperationContext = opCtx,
                }),
            };
            var expectedBlobMetadata = JsonHelpers.JsonToJObject("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}", true);
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync((JObject)null);
            Mock.Get(_storageService)
                .Setup(x => x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(false);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);
            Mock.Get(_logger)
                .Setup(x => x.LogEventObject(
                    out appInsightsUri,
                    LogEventIds.NoSuchBlobInBlobDeleteHandler,
                    It.IsAny<object>()));
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    LogEventIds.GridwichUnhandledException,
                    It.IsAny<Exception>(),
                    It.IsAny<object>()));

            // Act:
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert:
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(out appInsightsUri, LogEventIds.NoSuchBlobInBlobDeleteHandler, It.IsAny<object>()),
                Times.Once,
                "A warning should be logged when the blob is missing [1]");
            Mock.Get(_storageService).Verify(x =>
                x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_storageService).Verify(x =>
                x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to delete the blob.");
            Mock.Get(_eventGridPublisher)
                .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Exactly(2),
                "Should only publish ACK + Failure.");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseFailure);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseFailureDTO));
            var data = (ResponseFailureDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
            data.HandlerId.ShouldBe(_handler.GetHandlerId(), StringCompareShould.IgnoreCase);
            // TODO: likely needs adjustment after refactoring of failure events
        }

        /// <summary>Verify that we can still delete a blob that has no metadata</summary>
        [Fact]
        public async Task DoWorkAsync_Should_Return_Success_When_Blob_Present_But_No_Metadata()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var opCtx = JsonHelpers.JsonToJObject("{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}");

            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobDelete,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobDeleteDTO
                {
                    BlobUri = new Uri(_expectedInboxUrl),
                    OperationContext = opCtx,
                }),
            };

            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync((JObject)null);
            Mock.Get(_storageService)
                .Setup(x => x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act:
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert:
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            // Check that nothing (informational okay) was logged.
            // No exceptions or exception objects
            Mock.Get(_logger).Verify(x => x.LogException(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never,
                "No exceptions");
            Mock.Get(_logger).Verify(x => x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "No exception objects");

            // No criticals, note the specific LogEventIds don't matter here
            Mock.Get(_logger).Verify(x =>
                x.LogEvent(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.NoSuchBlobInBlobDeleteHandler.GetLevelName()), It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never,
                "No Criticals [1]");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.NoSuchBlobInBlobDeleteHandler.GetLevelName()), It.IsAny<object>()),
                Times.Never,
                "No Criticals [2]");

            // No Errors, note the specific LogEventIds don't matter here
            Mock.Get(_logger).Verify(x =>
                x.LogEvent(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler.GetLevelName()), It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never,
                "No Errors [1]");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler.GetLevelName()), It.IsAny<object>()),
                Times.Never,
                "No Errors [2]");

            // No warning that the blob doesn't exist, note the specific LogEventIds don't matter here
            Mock.Get(_logger).Verify(x =>
                x.LogEvent(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.NoSuchBlobInBlobDeleteHandler.GetLevelName()), It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never,
                "No Warnings [1]");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(It.Is<EventId>(e => e.GetLevelName() == LogEventIds.NoSuchBlobInBlobDeleteHandler.GetLevelName()), It.IsAny<object>()),
                Times.Never,
                "No Warnings [2]");

            Mock.Get(_storageService).Verify(x =>
                x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_storageService).Verify(x =>
                x.BlobDelete(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to delete the blob.");
            Mock.Get(_eventGridPublisher)
                .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Exactly(2),
                "Should only publish ACK + Failure.");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobDeleteScheduled);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobDeleteScheduledDTO));
            var data = (ResponseBlobDeleteScheduledDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
            data.BlobUri.ToString().ShouldBe(_expectedInboxUrl);
            // Metadata should be empty
            data.BlobMetadata.ShouldNotBeNull();
            data.BlobMetadata.Count.ShouldBe(0);  // empty JObject.
        }
    }
}
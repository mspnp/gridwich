using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.EventGridHandlers;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests.EventGridHandlers
{
    /// <summary>
    /// Test class MediaServicesV3EncoderStatusHandlerTests <see cref="MediaServicesV3EncoderStatusHandler"/> class.
    /// </summary>
    public class MediaServicesV3EncoderStatusHandlerTests
    {
        private readonly IObjectLogger<MediaServicesV3EncoderStatusHandler> logger;
        private readonly IEventGridPublisher eventGridPublisher;
        private readonly MediaServicesV3EncoderStatusHandler handler;
        private readonly IStorageService storageService;

        // JobOutputProgress Event
        private readonly MediaJobOutputProgressEventData outputJobJobOutputProgressData = new MediaJobOutputProgressEventData()
        {
            JobCorrelationData = new Dictionary<string, string>()
                {
                    { "mediaServicesV3EncoderSpecificData", string.Empty },
                    { "inputAssetName", "myinputasset" },
                    { "outputAssetContainer", "https://test.com/path/myassetcontainer/" },
                    { "outputAssetName", "https://test.com/path/myassetcontainer/" },
                    { "operationContext", "{\r\n  \"anotherkey\": \"value3\",\r\n  \"anotherkey2\": \"value4\"\r\n}" },
                },
            Label = "VideoAnalyzerPreset_0",
            Progress = 86,
        };

        private readonly EventGridEvent eventJobOutputProgressToPublish = new EventGridEvent
        {
            Topic = "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
            Subject = "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
            EventType = "Microsoft.Media.JobOutputProgress",
            EventTime = DateTime.Now,
            Id = "00000000-0000-0000-0000-000000000000",
            Data = null,
            DataVersion = "1.0",
        };

        // JobFinished Event
        private readonly MediaJobFinishedEventData outputJobFinishedData = new MediaJobFinishedEventData(
            correlationData: new Dictionary<string, string>()
            {
                { "mediaServicesV3EncoderSpecificData", string.Empty },
                { "inputAssetName", "myinputasset" },
                { "outputAssetContainer", "https://test.com/path/myassetcontainer/" },
                { "outputAssetName", "https://test.com/path/myassetcontainer/" },
                { "operationContext", "{\r\n  \"anotherkey\": \"value3\",\r\n  \"anotherkey2\": \"value4\"\r\n}" },
            },
            outputs: new[] { new MediaJobOutput(progress: 100, state: MediaJobState.Finished, error: null, label: null) },
            previousState: MediaJobState.Processing,
            state: MediaJobState.Finished);

        private readonly EventGridEvent eventJobFinishedToPublish = new EventGridEvent
        {
            Topic = "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
            Subject = "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
            EventType = "Microsoft.Media.JobFinished",
            EventTime = DateTime.Now,
            Id = "00000000-0000-0000-0000-000000000000",
            Data = null,
            DataVersion = "1.0",
        };

        // JobErrored Event
        private readonly MediaJobErroredEventData outputJobErroredData = new MediaJobErroredEventData(
            correlationData: new Dictionary<string, string>()
            {
                { "mediaServicesV3EncoderSpecificData", string.Empty },
                { "inputAssetName", "myinputasset" },
                { "outputAssetContainer", "https://test.com/path/myassetcontainer/" },
                { "outputAssetName", "https://test.com/path/myassetcontainer/" },
                { "operationContext", "{\r\n  \"anotherkey\": \"value3\",\r\n  \"anotherkey2\": \"value4\"\r\n}" },
            },
            outputs: new[] { new MediaJobOutput(progress: 100, state: MediaJobState.Finished, error: new MediaJobError(MediaJobErrorCode.UploadTransientError, "error in upload", MediaJobErrorCategory.Upload, MediaJobRetry.DoNotRetry, new List<MediaJobErrorDetail>() { new MediaJobErrorDetail("ErrorDownloadingInputAssetServiceFailure", "An error has occurred. Stage: DownloadFile. Code: System.IO.InvalidDataException.") }), label: null) },
            previousState: MediaJobState.Processing,
            state: MediaJobState.Error);

        private readonly EventGridEvent eventJobErroredToPublish = new EventGridEvent
        {
            Topic = "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
            Subject = "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
            EventType = "Microsoft.Media.JobErrored",
            EventTime = DateTime.Now,
            Id = "00000000-0000-0000-0000-000000000000",
            Data = null,
            DataVersion = "1.0",
        };

        // JobScheduled Event
        private readonly MediaJobScheduledEventData outputJobScheduledData = new MediaJobScheduledEventData(
       previousState: MediaJobState.Processing,
       state: MediaJobState.Scheduled,
       correlationData: new Dictionary<string, string>()
       {
                { "mediaServicesV3EncoderSpecificData", string.Empty },
                { "inputAssetName", "myinputasset" },
                { "outputAssetContainer", "https://test.com/path/myassetcontainer/" },
                { "outputAssetName", "https://test.com/path/myassetcontainer/" },
                { "operationContext", "{\r\n  \"anotherkey\": \"value3\",\r\n  \"anotherkey2\": \"value4\"\r\n}" },
       });

        private readonly EventGridEvent eventJobScheduledToPublish = new EventGridEvent
        {
            Topic = "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
            Subject = "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
            EventType = "Microsoft.Media.JobScheduled",
            EventTime = DateTime.Now,
            Id = "00000000-0000-0000-0000-000000000000",
            Data = null,
            DataVersion = "1.0",
        };


        // JobCanceled Event
        private readonly MediaJobCanceledEventData outputJobCanceledData = new MediaJobCanceledEventData(
            previousState: MediaJobState.Processing,
            state: MediaJobState.Canceled,
            correlationData: new Dictionary<string, string>()
            {
                { "mediaServicesV3EncoderSpecificData", string.Empty },
                { "inputAssetName", "myinputasset" },
                { "outputAssetContainer", "https://test.com/path/myassetcontainer/" },
                { "outputAssetName", "https://test.com/path/myassetcontainer/" },
                { "operationContext", "{\r\n  \"anotherkey\": \"value3\",\r\n  \"anotherkey2\": \"value4\"\r\n}" },
            },
            outputs: new[] { new MediaJobOutput(error: null, label: "VideoAnalyzerPreset_0", progress: 100, state: MediaJobState.Finished) });

        private readonly EventGridEvent eventJobCanceledToPublish = new EventGridEvent
        {
            Topic = "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
            Subject = "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
            EventType = "Microsoft.Media.JobCanceled",
            EventTime = DateTime.Now,
            Id = "00000000-0000-0000-0000-000000000000",
            Data = null,
            DataVersion = "1.0",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3EncoderStatusHandlerTests"/> class.
        /// </summary>
        public MediaServicesV3EncoderStatusHandlerTests()
        {
            this.logger = Mock.Of<IObjectLogger<MediaServicesV3EncoderStatusHandler>>();
            this.eventGridPublisher = Mock.Of<IEventGridPublisher>();
            this.storageService = Mock.Of<IStorageService>();
            this.handler = new MediaServicesV3EncoderStatusHandler(this.logger, this.eventGridPublisher, this.storageService);
        }

        /// <summary>
        /// Initialize expectedHandlerId and actualHandlerId.
        /// </summary>
        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "2229453D-58CF-4F16-A2FF-19647F6CBF81";

            // Act
            var actualHandlerId = this.handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        /// <summary>
        /// Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        /// <param name="shouldbe">Expected return value.</param>
        [Theory]
        [InlineData(EventTypes.MediaJobScheduledEvent, "1.0", true)]
        [InlineData(EventTypes.MediaJobScheduledEvent, "2.0", true)]
        [InlineData(EventTypes.MediaJobFinishedEvent, "1.0", true)]
        [InlineData(EventTypes.MediaJobFinishedEvent, "2.0", true)]
        [InlineData(EventTypes.MediaJobCanceledEvent, "1.0", true)]
        [InlineData(EventTypes.MediaJobCanceledEvent, "2.0", true)]
        [InlineData(EventTypes.MediaJobErroredEvent, "1.0", true)]
        [InlineData(EventTypes.MediaJobErroredEvent, "2.0", true)]
        [InlineData(EventTypes.MediaJobOutputProgressEvent, "1.0", true)]
        [InlineData(EventTypes.MediaJobOutputProgressEvent, "2.0", true)]
        public void HandlesEventShouldHandleEventTypeandVersion(string eventType, string dataVersion, bool shouldbe)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = this.handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBe(shouldbe);
        }

        /// <summary>
        /// Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        [Theory]
        [InlineData(CustomEventTypes.ResponseBlobAnalysisSuccess, "1.0")]
        [InlineData("NotAnExpectedEventType", "1.0")]
        [InlineData("NotAnExpectedEventType", "NotAnInt")]
        public void HandlesEventShouldNotHandleEventTypeandVersion(string eventType, string dataVersion)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = this.handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBeFalse();
        }

        /// <summary>
        /// JobOutputProgress Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobOutputProgressHandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            this.eventJobOutputProgressToPublish.Data = JObject.FromObject(this.outputJobJobOutputProgressData);

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobOutputProgressToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }

        /// <summary>
        /// JobOutputProgress Event handling failed with BadJSON log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobOutputProgressHandleAsyncShouldReturnFalseAndLogWithBadJSON()
        {
            // Arrange
            this.eventJobOutputProgressToPublish.Data = JsonConvert.SerializeObject(new { lobName = "bbb.mp4", rofiles = "h264" });

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobOutputProgressToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad JSON");
        }

        /// <summary>
        /// JobOutputProgress Event handling failed with Bad data log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobOutputProgressHandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            this.eventJobOutputProgressToPublish.Data = "Bad Data";

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobOutputProgressToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad data");
        }

        /// <summary>
        /// JobFinished Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobFinishedHandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            this.eventJobFinishedToPublish.Data = JObject.FromObject(this.outputJobFinishedData);
            outputJobFinishedData.CorrelationData.TryGetValue("outputAssetContainer", out var containerUri);
            EventGridEvent publishedEvent = null;
            string blobName = "covfefe";
            BlobItemProperties blobProps = BlobsModelFactory.BlobItemProperties(true);
            var blobArray = new BlobItem[]
            {
                BlobsModelFactory.BlobItem(blobName, false, blobProps)
            };
            var blobsList = new List<BlobItem>(blobArray);

            // Arrange Mocks
            Mock.Get(storageService)
                .Setup(x => x.ListBlobsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobsList);
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((ege) => publishedEvent = ege)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobFinishedToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            publishedEvent.Data.ShouldBeOfType<ResponseEncodeSuccessDTO>();
            var responseEncodeSuccessData = (ResponseEncodeSuccessDTO)publishedEvent.Data;
            for (int i = 0; i < blobArray.Length; i++)
            {
                responseEncodeSuccessData.Outputs[i].BlobUri.ShouldBe(new BlobUriBuilder(new Uri(containerUri)) { BlobName = blobArray[i].Name }.ToString());
            }
        }

        /// <summary>
        /// JobFinished Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobFinishedHandleAsyncShouldReturnTrueAndNotLogWhenNoErrorsAndNoBlobs()
        {
            // Arrange
            this.eventJobFinishedToPublish.Data = JObject.FromObject(this.outputJobFinishedData);
            outputJobFinishedData.CorrelationData.TryGetValue("outputAssetContainer", out var containerUri);
            EventGridEvent publishedEvent = null;
            var blobsList = new List<BlobItem>();

            // Arrange Mocks
            Mock.Get(storageService)
                .Setup(x => x.ListBlobsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobsList);
            Mock.Get(this.eventGridPublisher).Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((ege) => publishedEvent = ege)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobFinishedToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            publishedEvent.Data.ShouldBeOfType<ResponseEncodeSuccessDTO>();
            var responseEncodeSuccessData = (ResponseEncodeSuccessDTO)publishedEvent.Data;
            responseEncodeSuccessData.Outputs.Length.ShouldBe(0);
        }

        /// <summary>
        /// JobFinished Event handling failed with BadJSON log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobFinishedHandleAsyncShouldReturnFalseAndLogWithBadJSON()
        {
            // Arrange
            this.eventJobFinishedToPublish.Data = JsonConvert.SerializeObject(new { lobName = "bbb.mp4", rofiles = "h264" });

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobFinishedToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad JSON");
        }

        /// <summary>
        /// JobFinished Event handling failed with Bad data log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobFinishedHandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            this.eventJobFinishedToPublish.Data = "Bad Data";

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobFinishedToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad data");
        }

        /// <summary>
        /// JobErrored Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobErroredHandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            this.eventJobErroredToPublish.Data = JObject.FromObject(this.outputJobErroredData);
            var appInsightsUri = new Uri("https://www.appinsights.com");

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);
            Mock.Get(logger)
                .Setup(x => x.LogEventObject(
                    out appInsightsUri,
                    LogEventIds.MediaServicesV3JobErroredReceived,
                    It.IsAny<object>()));

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobErroredToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }

        /// <summary>
        /// JobErrored Event handling failed with BadJSON log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobErroredHandleAsyncShouldReturnFalseAndLogWithBadJSON()
        {
            // Arrange
            this.eventJobErroredToPublish.Data = JsonConvert.SerializeObject(new { lobName = "bbb.mp4", rofiles = "h264" });

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobErroredToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad JSON");
        }

        /// <summary>
        /// JobErrored Event handling failed with Bad data log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobErroredHandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            this.eventJobErroredToPublish.Data = "Bad Data";

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobErroredToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad data");
        }

        /// <summary>
        /// JobCanceled Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobCanceledHandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            this.eventJobCanceledToPublish.Data = JObject.FromObject(this.outputJobCanceledData);
            var appInsightsUri = new Uri("https://www.appinsights.com");

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);
            Mock.Get(logger)
                .Setup(x => x.LogEventObject(
                    out appInsightsUri,
                    LogEventIds.MediaServicesV3JobCanceledReceived,
                    It.IsAny<object>()));

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobCanceledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }

        /// <summary>
        /// JobCanceled Event handling failed with BadJSON log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobCanceledHandleAsyncShouldReturnFalseAndLogWithBadJSON()
        {
            // Arrange
            this.eventJobCanceledToPublish.Data = JsonConvert.SerializeObject(new { lobName = "bbb.mp4", rofiles = "h264" });

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobCanceledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad JSON");
        }

        /// <summary>
        /// JobCanceled Event handling failed with Bad data log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobCanceledHandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            this.eventJobCanceledToPublish.Data = "Bad Data";

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobCanceledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.AtLeastOnce,
                "An exception should be logged when the event Data field has bad data");
        }

        /// <summary>
        /// JobScheduled Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobScheduledHandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            this.eventJobScheduledToPublish.Data = JObject.FromObject(this.outputJobScheduledData);

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobScheduledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }

        /// <summary>
        /// JobScheduled Event handling failed with BadJSON log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobScheduledHandleAsyncShouldReturnFalseAndLogWithBadJSON()
        {
            // Arrange
            this.eventJobScheduledToPublish.Data = JsonConvert.SerializeObject(new { lobName = "bbb.mp4", rofiles = "h264" });

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobScheduledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad JSON");
        }

        /// <summary>
        /// JobScheduled Event handling failed with Bad data log <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <returns>return Task.</returns>
        [Fact]
        public async Task JobScheduledHandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            this.eventJobScheduledToPublish.Data = "Bad Data";

            // Arrange Mocks
            Mock.Get(this.eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await this.handler.HandleAsync(this.eventJobScheduledToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri appInsightUri;
            Mock.Get(this.logger).Verify(
                x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the event Data field has bad data");
        }
    }
}

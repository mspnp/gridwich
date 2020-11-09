using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2Tests
{
    /// <summary>
    /// Tests for the Media Services V2 Service class implementation.
    /// </summary>
    public class MediaServicesV2CallbackHandlerTests
    {
        private readonly IObjectLogger<MediaServicesV2CallbackHandler> log = Mock.Of<IObjectLogger<MediaServicesV2CallbackHandler>>();

        [Theory]
        [InlineData("Queued", "Scheduled", CustomEventTypes.ResponseEncodeMediaServicesV2Scheduled)]
        [InlineData("Processing", "Finished", CustomEventTypes.ResponseEncodeMediaServicesV2Success)]
        [InlineData("Canceling", "Canceled", CustomEventTypes.ResponseEncodeMediaServicesV2Canceled)]
        [InlineData("Processing", "Error", CustomEventTypes.ResponseFailure)]
        public async void HandleAsync_ShouldReturnTrue_WhenTaskStateChangePayloadIsValid(string oldState, string newState, string expectedEventType)
        {
            var mediaV2ServiceMock = Mock.Of<IMediaServicesV2EncodeService>();
            Mock.Get(mediaV2ServiceMock)
                .Setup(x => x.GetOperationContextForJobAsync(It.IsAny<string>()))
                .ReturnsAsync(new JObject());
            var egPublisherMock = Mock.Of<IEventGridPublisher>();

            dynamic opcontext = new JObject();
            opcontext.TestKey = "TestValue";

            Mock.Get(egPublisherMock)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            var handler = new MediaServicesV2CallbackHandler(log, mediaV2ServiceMock, egPublisherMock);

            var notificationMessage = new MediaServicesV2NotificationMessage()
            {
                ETag = "random",
                EventType = MediaServicesV2NotificationEventType.TaskStateChange,
                MessageVersion = "1.1",
                TimeStamp = DateTime.UtcNow,
                Properties = new Dictionary<string, string>()
                {
                    { "jobId", "nb:jid:UUID:91ab2b3a-0d00-a812-94e1-f1ea63c713e1" },
                    { "taskId", "nb:tid:UUID:4c1e6189-cb33-45ca-901a-46dcd3cc5f40" },
                    { "newState", newState },
                    { "oldState", oldState },
                    { "accountName", "justatest" },
                    { "accountId", "00000000-0000-0000-0000-000000000000" },
                    { "notificationEndPointId", "nb:nepid:UUID:738eb6b5-a18d-4b7f-961d-bc33e844da13" }
                }
            };

            var actual = await handler.HandleAsync(GetEventFromNotificationMessage(notificationMessage)).ConfigureAwait(false);
            actual.ShouldBeTrue();

            Mock.Get(egPublisherMock)
                .Verify(x => x.PublishEventToTopic(It.Is<EventGridEvent>(x => x.EventType == expectedEventType)),
                    Times.Once,
                    "publish should be called with the correct eventtype");
        }

        [Fact]
        public async void HandleAsync_ShouldPublishFailureEvent_WhenPayloadIsNotFormatedCorrectly()
        {
            var mediaV2ServiceMock = Mock.Of<IMediaServicesV2EncodeService>();
            var egPublisherMock = Mock.Of<IEventGridPublisher>();

            dynamic opcontext = new JObject();
            opcontext.TestKey = "TestValue";

            Mock.Get(egPublisherMock)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            var handler = new MediaServicesV2CallbackHandler(log, mediaV2ServiceMock, egPublisherMock);

            var notificationMessage = new MediaServicesV2NotificationMessage()
            {
                ETag = "random",
                EventType = MediaServicesV2NotificationEventType.TaskStateChange,
                MessageVersion = "1.1",
                TimeStamp = DateTime.UtcNow,
                Properties = new Dictionary<string, string>()
                {
                    { "jobId", "nb:jid:UUID:91ab2b3a-0d00-a812-94e1-f1ea63c713e1" },
                    { "taskId", "nb:tid:UUID:4c1e6189-cb33-45ca-901a-46dcd3cc5f40" },
                    { "noState", "Wrong" },
                    { "oldState", "OldState" },
                    { "accountName", "justatest" },
                    { "accountId", "00000000-0000-0000-0000-000000000000" },
                    { "notificationEndPointId", "nb:nepid:UUID:738eb6b5-a18d-4b7f-961d-bc33e844da13" }
                }
            };

            await handler.HandleAsync(GetEventFromNotificationMessage(notificationMessage)).ConfigureAwait(false);

            Mock.Get(egPublisherMock)
                .Verify(x => x.PublishEventToTopic(It.Is<EventGridEvent>(e => e.EventType == CustomEventTypes.ResponseFailure)));
        }

        private static EventGridEvent GetEventFromNotificationMessage(MediaServicesV2NotificationMessage notificationMessage)
        {
            var eventGridEventId = System.Guid.NewGuid().ToString();

            return new EventGridEvent
            {
                Id = eventGridEventId,
                Data = JsonConvert.SerializeObject(notificationMessage),
                EventTime = System.DateTime.UtcNow,
                EventType = CustomEventTypes.ResponseEncodeMediaServicesV2TranslateCallback,
                Subject = $"/{CustomEventTypes.ResponseEncodeMediaServicesV2TranslateCallback}/{eventGridEventId}",
                DataVersion = "1.0",
            };
        }
    }
}
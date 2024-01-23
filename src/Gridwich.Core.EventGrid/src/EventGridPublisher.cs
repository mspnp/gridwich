using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.Core.EventGrid
{
    /// <summary>
    /// Provides a way to publish events to an event grid topic.
    /// </summary>
    public class EventGridPublisher : IEventGridPublisher
    {
        private readonly IObjectLogger<EventGridPublisher> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IEventGridClientProvider _eventGridClientProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="settingsProvider">settingsProvider.</param>
        /// <param name="eventGridClientProvider">eventGridClientProvider.</param>
        public EventGridPublisher(
            IObjectLogger<EventGridPublisher> logger,
            ISettingsProvider settingsProvider,
            IEventGridClientProvider eventGridClientProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _eventGridClientProvider = eventGridClientProvider;
        }

        /// <summary>
        /// Publishes the provided event to a topic. Topic configuration will be fetched from the application settings using this object's
        /// <see cref="ISettingsProvider"/>.
        /// </summary>
        /// <param name="eventToPublish">The event to be published.</param>
        /// <returns>A boolean indicating if the operation was run successfully.</returns>
        /// <exception cref="ArgumentException">If the configuration for the topic cannot be found in the settings.</exception>
        public async Task<bool> PublishEventToTopic(EventGridEvent eventToPublish)
        {
            _ = eventToPublish ?? throw new ArgumentNullException(nameof(eventToPublish));

            var eventInfo = new { eventToPublish.Id, eventToPublish.EventType, eventToPublish.DataVersion };
            try
            {
                var topicKey = _settingsProvider.GetAppSettingsValue(Publishing.TopicOutboundKeySettingName);
                _ = topicKey ?? throw new ArgumentException($"No topic key was found for topic with name {Publishing.TopicOutbound}");

                var topicEndPointString = _settingsProvider.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName);
                _ = topicEndPointString ?? throw new ArgumentException($"No topic endpoint was found for topic with name {Publishing.TopicOutbound}");

                Uri.TryCreate(topicEndPointString, UriKind.Absolute, out Uri topicEndPoint);
                _ = topicEndPoint ?? throw new ArgumentException($"Failed to parse Uri for topic with name {Publishing.TopicOutbound}");

                var eventGridClient = _eventGridClientProvider.GetEventGridClientForTopic(Publishing.TopicOutbound, topicKey);

                _logger.LogEventObject(LogEventIds.AboutToAttemptPublishOfEventWithId, eventInfo);
                await eventGridClient.PublishEventsAsync(topicEndPoint.Host, new List<EventGridEvent>() { eventToPublish }).ConfigureAwait(false);
                _logger.LogEventObject(LogEventIds.PublishedEvent, eventInfo);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogExceptionObject(LogEventIds.ExceptionPublishingEvent, e, eventInfo);
                return false;
            }
        }
    }
}
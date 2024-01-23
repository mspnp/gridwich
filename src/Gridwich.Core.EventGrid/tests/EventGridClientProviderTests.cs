using Gridwich.Core.EventGrid;
using Gridwich.Core.Helpers;
using LazyCache;
using Microsoft.Azure.EventGrid;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace Gridwich.Core.EventGridTests
{
    public class EventGridClientProviderTests
    {
        private readonly EventGridClientProvider _eventGridClientProvider;
        private readonly IAppCache _eventGridClientCache;

        public EventGridClientProviderTests()
        {
            _eventGridClientCache = new CachingService();
            _eventGridClientProvider = new EventGridClientProvider(_eventGridClientCache);
        }

        [Fact]
        public void GetIEventGridClientForTopic_ShouldThrow_WhenNullTopicName()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _eventGridClientProvider.GetEventGridClientForTopic(null, "KEY"));
        }

        [Fact]
        public void GetIEventGridClientForTopic_ShouldThrow_WhenNullTopicKey()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _eventGridClientProvider.GetEventGridClientForTopic("NAME", null));
        }

        [Fact]
        public void GetIEventGridClientForTopic_ShouldReturnNewClient_WhenNotInDictionary()
        {
            // Arrange
            const string topic1Name = "TOPIC1_NAME";
            const string topic1Key = "TOPIC1_KEY";
            const string topic2Name = "TOPIC2_NAME";
            const string topic2Key = "TOPIC2_KEY";
            var eventGridClient = Mock.Of<IEventGridClient>();
            _eventGridClientCache.Add(EventGridClientProvider.GetCacheKeyForEventGridClient(topic1Name, topic1Key), eventGridClient);

            // Act
            var newClient = _eventGridClientProvider.GetEventGridClientForTopic(topic2Name, topic2Key);
            var existingClient = _eventGridClientProvider.GetEventGridClientForTopic(topic1Name, topic1Key);

            // Assert
            newClient.ShouldBeOfType<EventGridClient>();
            newClient.SerializationSettings.ContractResolver.ShouldBeEquivalentTo(JsonHelpers.GridwichSerializerSettings.ContractResolver);
            newClient.ShouldNotBe(eventGridClient);

            existingClient.ShouldBe(eventGridClient);
        }

        [Fact]
        public void GetIEventGridClientForTopic_ShouldReturnExistingClient_WhenAlreadyInDictionary()
        {
            // Arrange
            const string topicName = "TOPIC_NAME";
            const string topicKey = "TOPIC_KEY";
            var eventGridClient = Mock.Of<IEventGridClient>();
            _eventGridClientCache.Add(EventGridClientProvider.GetCacheKeyForEventGridClient(topicName, topicKey), eventGridClient);

            // Act
            var result = _eventGridClientProvider.GetEventGridClientForTopic(topicName, topicKey);

            // Assert
            result.ShouldBe(eventGridClient);
        }
    }
}
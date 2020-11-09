using System;

using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;

using LazyCache;

using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gridwich.Core.EventGridTests")]

namespace Gridwich.Core.EventGrid
{
    /// <summary>
    /// Provides one event grid client per topic. This class will maintain a cache of reusable clients.
    /// </summary>
    public class EventGridClientProvider : IEventGridClientProvider
    {
        private static readonly TimeSpan ClientExpirationTime = new TimeSpan(6, 0, 0);
        private static readonly string CacheKeyPrefix = $"{typeof(EventGridClientProvider)}-GetEventGridClientForTopic";
        private readonly IAppCache _eventGridClientCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridClientProvider"/> class.
        /// </summary>
        /// <param name="appCache">appCache.</param>
        public EventGridClientProvider(IAppCache appCache)
        {
            _eventGridClientCache = appCache;
        }

        /// <summary>
        /// Searches by topic name for a client in the cache or creates a new one and returns it. It the topic key changes
        /// for a specific topic name that is already cached, the old instance will be discarded and a new one will be cached with
        /// the new topic key.
        /// </summary>
        /// <param name="topicName">The name of the topic.</param>
        /// <param name="topicKey">The key for the topic.</param>
        /// <returns>An event grid client whose credentials are tied to the provided topic key.</returns>
        /// <exception cref="ArgumentNullException">If either the topic name or key are null.</exception>
        public IEventGridClient GetEventGridClientForTopic(string topicName, string topicKey)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            if (topicKey == null)
            {
                throw new ArgumentNullException(nameof(topicKey));
            }

            // We'll simplify using a cache that will handle locking and concurrency and also evict unused clients
            // after a set sliding expiration time
            return _eventGridClientCache.GetOrAdd(GetCacheKeyForEventGridClient(topicName, topicKey),
                () => CreateEventGridClientForTopic(topicKey),
                ClientExpirationTime);
        }

        /// <summary>
        /// Returns unique cache key.
        /// </summary>
        /// <param name="topicName">topicName.</param>
        /// <param name="topicKey">topicKey.</param>
        /// <returns>unique cache key.</returns>
        internal static string GetCacheKeyForEventGridClient(string topicName, string topicKey)
        {
            return $"{CacheKeyPrefix}-{topicName}-{topicKey}";
        }

        private static IEventGridClient CreateEventGridClientForTopic(string topicKey)
        {
            var topicCredentials = new TopicCredentials(topicKey);

            // The client already uses a ReadOnlyJsonContractResolver but we want camel cased json,
            // so we override their default JSON settings to match those used across Gridwich.
            var eventGridClient = new EventGridClient(topicCredentials);

            JsonHelpers.ResetSerializationSettingsForGridwich(eventGridClient.SerializationSettings);
            JsonHelpers.ResetSerializationSettingsForGridwich(eventGridClient.DeserializationSettings);

            return eventGridClient;
        }
    }
}
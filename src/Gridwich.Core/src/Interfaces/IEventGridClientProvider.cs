using Microsoft.Azure.EventGrid;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// EventGridClientProvider interface
    /// </summary>
    public interface IEventGridClientProvider
    {
        /// <summary>
        /// Gets or creates and returns an event grid client for the provided topic.
        /// </summary>
        /// <param name="topicName">Name of the topic.</param>
        /// <param name="topicKey">The topic key.</param>
        /// <returns>
        /// IEventGridClient
        /// </returns>
        public IEventGridClient GetEventGridClientForTopic(string topicName, string topicKey);
    }
}
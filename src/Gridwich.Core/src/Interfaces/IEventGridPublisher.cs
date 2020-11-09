using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Microsoft.Azure.EventGrid.Models;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// Publishes EventGrid events on behalf of IEventHandlers
    /// </summary>
    public interface IEventGridPublisher
    {
        /// <summary>
        /// Publishes event to the topic name.
        /// </summary>
        /// <param name="eventToPublish">The event to publish.</param>
        /// <returns>
        /// true if the event was successfully published.
        /// </returns>
        Task<bool> PublishEventToTopic(EventGridEvent eventToPublish);
    }
}
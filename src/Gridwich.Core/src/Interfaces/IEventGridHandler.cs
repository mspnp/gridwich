using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// All EventGrid handlers implement this interface.
    /// Their implementations should be registered with the dependency-injection system
    /// in the startup class of the hosting framework.
    /// </summary>
    public interface IEventGridHandler
    {
        /// <summary>
        /// The EventGridHandler must explicitly op-in to handling the event type and data version.
        /// The EventType should be one of:
        ///   CustomEventTypes.*
        ///   Microsoft.Azure.EventGrid.EventTypes.*
        /// </summary>
        /// <param name="eventType">EventGridEvent.EventType</param>
        /// <param name="dataVersion">EventGridEvent.DataVersion</param>
        /// <returns>Can handle</returns>
        bool HandlesEvent(string eventType, string dataVersion);

        /// <summary>
        /// Handles the EventGridEvent.
        /// </summary>
        /// <param name="eventGridEvent">The event grid event.</param>
        /// <returns>
        /// True or false if handled successfully. Throws exceptions otherwise.
        /// </returns>
        Task<bool> HandleAsync(EventGridEvent eventGridEvent);

        /// <summary>
        /// Get the unique identifier for this handler.
        /// </summary>
        /// <returns>A string that is unique for this identifier.</returns>
        string GetHandlerId();
    }
}
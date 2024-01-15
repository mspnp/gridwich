using Microsoft.Azure.EventGrid.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// EventGridDispatcher interface
    /// </summary>
    public interface IEventGridDispatcher
    {
        /// <summary>
        /// Dispatches one or more events.  Answers EventGridSubscriptionValidationEvent as needed.
        /// Usage: Pass in the collection of EventGrid events and cast the result into IActionResult
        /// </summary>
        /// <param name="eventGridEvents">The event grid events.</param>
        /// <returns>
        /// IActionResult
        /// </returns>
        public Task<object> DispatchEventGridEvents(List<EventGridEvent> eventGridEvents);
    }
}
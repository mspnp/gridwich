using Microsoft.Azure.EventGrid.Models;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// Decorates an <see cref="EventGridEvent"/> with additional metadata
    /// </summary>
    public interface IEventDecorator
    {
        /// <summary>
        /// Decorates the specified EventGridEvent.
        /// </summary>
        /// <param name="e">The EventGridEvent to decorate.</param>
        /// <returns>The decorated EventGridEvent</returns>
        EventGridEvent Decorate(EventGridEvent e);
    }
}

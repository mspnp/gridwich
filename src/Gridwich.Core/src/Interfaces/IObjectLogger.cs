using System;

using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// We force the use the EventIds here, so that all events have proper Ids going into application insights.
    /// The Log__Object methods provide a simplified wrapper to allow for easy logging of entire objects to
    /// Application Insights.
    /// </summary>
    /// <typeparam name="T">Type of the class calling the IObjectLogger</typeparam>
    public interface IObjectLogger<T>
    {
        /// <summary>
        /// Logs the event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogEvent(EventId eventId, string message, params object[] args);

        /// <summary>
        /// Logs the event.
        /// </summary>
        /// <param name="locator">The locator.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogEvent(out Uri locator, EventId eventId, string message, params object[] args);

        /// <summary>
        /// Logs the event object.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="o">The o.</param>
        void LogEventObject(EventId eventId, object o);

        /// <summary>
        /// Logs the event object.
        /// </summary>
        /// <param name="locator">The locator.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="o">The o.</param>
        void LogEventObject(out Uri locator, EventId eventId, object o);

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogException(EventId eventId, Exception ex, string message, params object[] args);

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="locator">The locator.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogException(out Uri locator, EventId eventId, Exception ex, string message, params object[] args);

        /// <summary>
        /// Logs the exception object.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="o">The o.</param>
        void LogExceptionObject(EventId eventId, Exception ex, object o);

        /// <summary>
        /// Logs the exception object.
        /// </summary>
        /// <param name="locator">The locator.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="o">The o.</param>
        void LogExceptionObject(out Uri locator, EventId eventId, Exception ex, object o);
    }
}
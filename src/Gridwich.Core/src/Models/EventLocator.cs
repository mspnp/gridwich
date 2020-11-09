using System;
using Gridwich.Core.Interfaces;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Models
{
    /// <summary>
    /// The ID and locating URL for a unique event re
    /// </summary>
    public class EventLocator : IEventDecorator
    {
        private readonly IAppInsightsUrlCreator _urlCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLocator"/> class.
        /// </summary>
        /// <param name="urlCreator">The URL creator.</param>
        public EventLocator(IAppInsightsUrlCreator urlCreator) => _urlCreator = urlCreator;

        /// <summary>
        /// Gets the log record identifier.
        /// </summary>
        public string LogRecordId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the log record URL.
        /// </summary>
        public Uri LogRecordUrl => _urlCreator.CreateUrl($@"customEvents | where logRecordId = ""{this.LogRecordId}""");

        /// <summary>
        /// Makes the specified EventGridEvent locatable by adding 'LogRecordId' and 'LogRecordUrl' to its data
        /// </summary>
        /// <param name="e">The EventGridEvent to decorate.</param>
        /// <returns>
        /// The decorated EventGridEvent
        /// </returns>
        public EventGridEvent Decorate(EventGridEvent e)
        {
            _ = e ?? throw new ArgumentNullException(nameof(e));

            var dataObject = JObject.FromObject(e.Data);

            dataObject.Add(nameof(this.LogRecordId), this.LogRecordId);
            dataObject.Add(nameof(this.LogRecordUrl), this.LogRecordUrl);

            e.Data = dataObject;

            return e;
        }
    }
}

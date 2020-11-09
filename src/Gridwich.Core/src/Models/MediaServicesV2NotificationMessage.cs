using System;
using System.Collections.Generic;

namespace Gridwich.Core.Models
{
    /// <summary>
    /// Notification message recieved from AMS.V2.
    /// </summary>
    public class MediaServicesV2NotificationMessage
    {
        /// <summary>
        /// Gets or sets messageVersion.
        /// </summary>
        public string MessageVersion { get; set; }

        /// <summary>
        /// Gets or sets eTag.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets eventType.
        /// </summary>
        public MediaServicesV2NotificationEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets timeStamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets properties.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public IDictionary<string, string> Properties { get; set; }
    }
}

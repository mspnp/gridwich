namespace Gridwich.Core.Models
{
    /// <summary>
    /// The media services v2 notification event type
    /// </summary>
    public enum MediaServicesV2NotificationEventType
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        /// <summary>
        /// The job state change
        /// </summary>
        JobStateChange = 1,

        /// <summary>
        /// The notification end point registration
        /// </summary>
        NotificationEndPointRegistration = 2,

        /// <summary>
        /// The notification end point unregistration
        /// </summary>
        NotificationEndPointUnregistration = 3,

        /// <summary>
        /// The task state change
        /// </summary>
        TaskStateChange = 4,

        /// <summary>
        /// The task progress
        /// </summary>
        TaskProgress = 5
    }
}

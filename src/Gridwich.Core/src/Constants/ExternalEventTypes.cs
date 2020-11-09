namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Defines the EventGrid EventTypes coming from the Flip service.
    /// </summary>
    public static class ExternalEventTypes
    {
        /// <summary>
        /// encoding-complete
        /// </summary>
        public const string FlipEncodingComplete = "encoding-complete";

        /// <summary>
        /// encoding-progress
        /// </summary>
        public const string FlipEncodingProgress = "encoding-progress";

        /// <summary>
        /// video-created
        /// </summary>
        public const string FlipVideoCreated = "video-created";

        /// <summary>
        /// video-encoded
        /// </summary>
        public const string FlipVideoEncoded = "video-encoded";

        /// <summary>
        /// workflow created EventGrid message from Vantage CloudPort.
        /// </summary>
        public const string CloudPortWorkflowJobCreated = "workflow-job-created";

        /// <summary>
        /// workflow success EventGrid message from Vantage CloudPort.
        /// </summary>
        public const string CloudPortWorkflowJobSuccess = "workflow-job-success";

        /// <summary>
        /// workflow error EventGrid message from Vantage CloudPort.
        /// </summary>
        public const string CloudPortWorkflowJobError = "workflow-job-error";

        /// <summary>
        /// workflow progress EventGrid message from Vantage CloudPort.
        /// </summary>
        public const string CloudPortWorkflowJobProgress = "workflow-job-progress";
    }
}
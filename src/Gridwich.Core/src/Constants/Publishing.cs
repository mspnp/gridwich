namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Constants related to EventGrid publishing of inbound/outbound events.
    /// </summary>
    /// <remarks>
    /// For now, Gridwich will only use a single underlying EventGrid Topic.
    /// That said, we'll use two different constants to allow developer intent
    /// to be expressed in code (but both will still point to the single GRW topic).
    ///
    /// Notes:<ol>
    /// <li>Currently, Gridwich code only references outbound Topic names since inbound
    /// events are referred to the Azure Function app via EventGrid configuration - i.e.,
    /// EventGrid pushes to the Function app webHook.  So it may be that there are never
    /// in-code references to the inbound topic.  But for now, we'll confirm that's the
    /// case by seeing if the inbound constant below stays at zero references.
    ///
    /// <li>These constants do not specificially name the the EventGrid topic, but
    /// are instead used in the SetttingsProvider as the prefix of KeyVault items that
    /// contain the Topic Key and Endpoint values.
    /// </ol>
    /// </remarks>
    public static class Publishing
    {
        /// <summary>
        /// EventGrid topic identifier for request events inbound from Requestor and
        /// notifications from other services such as Azure Storage.
        /// </summary>
        public const string TopicInbound = "GRW";

        /// <summary>
        /// EventGrid topic identifier for publishing events outbound to Requestor.
        /// </summary>
        public const string TopicOutbound = "GRW";

        /// <summary>
        /// Gets the topic outbound endpoint setting name.
        /// </summary>
        public const string TopicOutboundEndpointSettingName = "GRW_TOPIC_END_POINT";

        /// <summary>
        /// Gets the topic outbound key setting name
        /// </summary>
        public const string TopicOutboundKeySettingName = "GRW_TOPIC_KEY";
    }
}
namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies
{
    /// <summary>
    /// Interface used to contain logic to load and retrieve streaming policies.
    /// </summary>
    public interface IMediaServicesV3CustomStreamingPolicyService
    {
        /// <summary>
        /// Method used to get a streamingy policy from the list of custom Gridwich policies.
        /// </summary>
        /// <param name="streamingPolicyName">Name of streaming policy</param>
        /// <returns>MediaServicesV3TransformBase. Null if not found.</returns>
        public MediaServicesV3CustomStreamingPolicyBase GetCustomStreamingPolicyFromMemory(string streamingPolicyName);
    }
}

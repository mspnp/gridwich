namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies
{
    /// <summary>
    /// Interface used to contain logic to load and retrieve content key policies.
    /// </summary>
    public interface IMediaServicesV3ContentKeyPolicyService
    {
        /// <summary>
        /// Method used to get a content key policy
        /// </summary>
        /// <param name="contentKeyPolicyName">Name of content key policy</param>
        /// <returns>MediaServicesV3ContentKeyPolicyBase</returns>
        public MediaServicesV3CustomContentKeyPolicyBase GetContentKeyPolicyFromMemory(string contentKeyPolicyName);
    }
}

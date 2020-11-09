using Gridwich.SagaParticipants.Publication.MediaServicesV3.Constants;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies
{
    /// <summary>
    /// Concrete class that contains the MultiDrmCencStreaming custom streaming policy.
    /// Supported DRM :
    /// - PlayReady
    /// - Widevine
    /// The policy is re-created if you change nameInAmsAccount below.
    /// </summary>
    public class MediaServicesV3CustomStreamingPolicyCencDrmStreaming : MediaServicesV3CustomStreamingPolicyBase
    {
        /// <summary>
        /// Name of the policy in the AMS account.
        /// Please change the end of the string by increasing the value of the version if you changed the settings and want the policy in AMS to be created and used for new locators.
        /// </summary>
        private readonly string nameInAmsAccount = CustomStreamingPolicies.CencDrmStreaming + "-Version-1-0";

        /// <summary>
        /// Name of the policy in Gridwich. Do not change.
        /// </summary>
        private readonly string name = CustomStreamingPolicies.CencDrmStreaming;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3CustomStreamingPolicyCencDrmStreaming"/> class.
        /// </summary>
        public MediaServicesV3CustomStreamingPolicyCencDrmStreaming()
            : base()
        {
            Create();
        }

        /// <inheritdoc cref="MediaServicesV3CustomStreamingPolicyBase"/>
        protected override void Create()
        {
            StreamingPolicy = GenerateStreamingPolicy();
            NameInAmsAccount = nameInAmsAccount;
            Name = name;
        }

        /// <summary>
        /// Method used to get the custom streaming policy.
        /// </summary>
        /// <returns>Custom streaming policy.</returns>
        private StreamingPolicy GenerateStreamingPolicy()
        {
            return new StreamingPolicy(name: nameInAmsAccount, commonEncryptionCenc: GenerateCencConfig());
        }
    }
}
using System;
using System.Collections.Generic;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Helpers;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies
{
    /// <inheritdoc/>
    public class MediaServicesV3CustomStreamingPolicyService : IMediaServicesV3CustomStreamingPolicyService
    {
        private Dictionary<string, MediaServicesV3CustomStreamingPolicyBase> _streamingPolicies;

        /// <inheritdoc/>
        public MediaServicesV3CustomStreamingPolicyBase GetCustomStreamingPolicyFromMemory(string streamingPolicyName)
        {
            MediaServicesV3ProtectionHelpers.CheckArgumentNotNullOrEmpty(streamingPolicyName, nameof(streamingPolicyName));

            // if first time
            if (_streamingPolicies == null)
            {
                _streamingPolicies = LoadStreamingPolicies();
            }

            // Search the dictionary
            if (!_streamingPolicies.TryGetValue(streamingPolicyName, out var streamingKeyPolicy))
            {
                streamingKeyPolicy = null;
            }

            return streamingKeyPolicy;
        }

        /// <summary>
        /// Method is used to load a list of streaming policies in memory.
        /// </summary>
        /// <returns>Dictionary with the policy name and policies</returns>
        private static Dictionary<string, MediaServicesV3CustomStreamingPolicyBase> LoadStreamingPolicies()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            return new Dictionary<string, MediaServicesV3CustomStreamingPolicyBase>(comparer)
            {
                // Load custom streaming policies
                { CustomStreamingPolicies.CencDrmStreaming, new MediaServicesV3CustomStreamingPolicyCencDrmStreaming() },
                { CustomStreamingPolicies.MultiDrmStreaming, new MediaServicesV3CustomStreamingPolicyMultiDrmStreaming() }
            };
        }
    }
}
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies
{
    /// <summary>
    /// Class will be used as a base class to create new custom streaming policies.
    /// </summary>
    public abstract class MediaServicesV3CustomStreamingPolicyBase
    {
        /// <summary>
        /// Gets or sets the name of streaming policy as stored in the Azure Media Services account.
        /// </summary>
        public string NameInAmsAccount { get; protected set; }

        /// <summary>
        /// Gets or sets the name of streaming policy as exposed in Gridwich.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the streaming policy parameters.
        /// </summary>
        public StreamingPolicy StreamingPolicy { get; protected set; }

        /// <summary>
        /// Method is used to create a streaming policy.
        /// </summary>
        protected abstract void Create();

        /// <summary>
        /// Generates de Cenc config for Dash and Smooth Streaming.
        /// </summary>
        /// <returns>The Cenc configuration.</returns>
        protected static CommonEncryptionCenc GenerateCencConfig()
        {
            return new CommonEncryptionCenc()
            {
                Drm = new CencDrmConfiguration()
                {
                  PlayReady = new StreamingPolicyPlayReadyConfiguration(),
                  Widevine = new StreamingPolicyWidevineConfiguration()
                },
                EnabledProtocols = new EnabledProtocols()
                {
                    Hls = false,
                    Dash = true,
                    SmoothStreaming = true,
                    Download = false
                },
                ContentKeys = new StreamingPolicyContentKeys()
                {
                    // Default key must be specified if keyToTrackMappings is present
                    DefaultKey = new DefaultKey()
                    {
                        Label = "cencKeyDefault"
                    }
                }
            };
        }

        /// <summary>
        /// Generates de Cbcs config with offline FairPlayfor HLS-Ts and HLS-Cmaf.
        /// </summary>
        /// <returns>The Cenc configuration.</returns>
        protected static CommonEncryptionCbcs GenerateCbcsConfig()
        {
            return new CommonEncryptionCbcs()
            {
                Drm = new CbcsDrmConfiguration()
                {
                    FairPlay = new StreamingPolicyFairPlayConfiguration()
                    {
                        AllowPersistentLicense = true // this enables offline mode
                    }
                },
                EnabledProtocols = new EnabledProtocols()
                {
                    Hls = true,
                    Dash = true // Even though DASH under CBCS is not supported for either CSF or CMAF, HLS-CMAF-CBCS uses DASH-CBCS fragments in its HLS playlist
                },

                ContentKeys = new StreamingPolicyContentKeys()
                {
                    // Default key must be specified if keyToTrackMappings is present
                    DefaultKey = new DefaultKey()
                    {
                        Label = "cbcsKeyDefault"
                    }
                }
            };
        }
    }
}
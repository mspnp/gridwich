using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Helpers;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies
{
    /// <inheritdoc/>
    public class MediaServicesV3ContentKeyPolicyService : IMediaServicesV3ContentKeyPolicyService
    {
        private readonly string _amsDrmOpenIdConnectDiscoveryDocument;
        private readonly string _amsDrmFairPlayPfxPassword;
        private readonly string _amsDrmFairPlayAskHex;
        private readonly string _amsDrmFairPlayCertificateB64;
        private readonly IObjectLogger<MediaServicesV3ContentKeyPolicyService> _log;
        private Dictionary<string, MediaServicesV3CustomContentKeyPolicyBase> _contentKeyPolicies;
        private string _amsDrmFairPlayPfx;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3ContentKeyPolicyService"/> class.
        /// </summary>
        /// <param name="settingsProvider">Settings provider.</param>
        /// <param name="log">Log provider.</param>
        /// <param name="identity">Identity provider.</param>
        public MediaServicesV3ContentKeyPolicyService(ISettingsProvider settingsProvider, IObjectLogger<MediaServicesV3ContentKeyPolicyService> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _ = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));

            _amsDrmOpenIdConnectDiscoveryDocument = settingsProvider.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument");
            _amsDrmFairPlayPfxPassword = settingsProvider.GetAppSettingsValue("AmsDrmFairPlayPfxPassword");
            _amsDrmFairPlayAskHex = settingsProvider.GetAppSettingsValue("AmsDrmFairPlayAskHex");
            _amsDrmFairPlayCertificateB64 = settingsProvider.GetAppSettingsValue("AmsDrmFairPlayCertificateB64");

            if (string.IsNullOrEmpty(_amsDrmOpenIdConnectDiscoveryDocument))
            {
                const string Message = "AmsDrmOpenIdConnectDiscoveryDocument is null or empty.";
                _log.LogEvent(LogEventIds.MediaServicesV3ConfigurationError, Message);
                throw new ArgumentException(Message);
            }

            if (string.IsNullOrEmpty(_amsDrmFairPlayPfxPassword))
            {
                const string Message = "AmsDrmFairPlayPfxPassword is null or empty.";
                _log.LogEvent(LogEventIds.MediaServicesV3ConfigurationError, Message);
                throw new ArgumentException(Message);
            }

            if (string.IsNullOrEmpty(_amsDrmFairPlayAskHex))
            {
                const string Message = "AmsDrmFairPlayAskHex is null or empty.";
                _log.LogEvent(LogEventIds.MediaServicesV3ConfigurationError, Message);
                throw new ArgumentException(Message);
            }

            if (string.IsNullOrEmpty(_amsDrmFairPlayCertificateB64))
            {
                const string Message = "AmsDrmFairPlayCertificateB64 is null or empty.";
                _log.LogEvent(LogEventIds.MediaServicesV3ConfigurationError, Message);
                throw new ArgumentException(Message);
            }
        }

        /// <summary>
        /// This method initialize the FairPlay certificate.
        /// It reads it from the app settings (base 64) and use pfx password.
        /// </summary>
        private void InitFairPlayCertificate()
        {
            try
            {
                using X509Certificate2 x509FairPlayCertificate = new X509Certificate2(Convert.FromBase64String(_amsDrmFairPlayCertificateB64), _amsDrmFairPlayPfxPassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
                if (!x509FairPlayCertificate.HasPrivateKey)
                {
                    var msg = "AMS v3 DRM Config. FairPlay certificate does not have a private key.";
                    throw new GridwichPublicationDRMConfigurationException(
                        msg,
                        null);
                }
                _amsDrmFairPlayPfx = Convert.ToBase64String(x509FairPlayCertificate.Export(X509ContentType.Pfx, _amsDrmFairPlayPfxPassword));
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 DRM Config. Error when loading the FairPlay certificate.";
                throw new GridwichPublicationDRMConfigurationException(
                    msg,
                    ex);
            }
        }

        /// <inheritdoc/>
        public MediaServicesV3CustomContentKeyPolicyBase GetContentKeyPolicyFromMemory(string contentKeyPolicyName)
        {
            MediaServicesV3ProtectionHelpers.CheckArgumentNotNullOrEmpty(contentKeyPolicyName, nameof(contentKeyPolicyName));

            // if first time
            if (_contentKeyPolicies == null)
            {
                InitFairPlayCertificate();
                _contentKeyPolicies = LoadContentKeyPolicies();
            }

            // Search the dictionary
            if (!_contentKeyPolicies.TryGetValue(contentKeyPolicyName, out var contentKeyPolicy))
            {
                contentKeyPolicy = null;
            }

            return contentKeyPolicy;
        }

        /// <summary>
        /// Method is used to load a list of content key policies in memory.
        /// </summary>
        /// <returns>Dictionary with the policy name and policies</returns>
        private Dictionary<string, MediaServicesV3CustomContentKeyPolicyBase> LoadContentKeyPolicies()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            return new Dictionary<string, MediaServicesV3CustomContentKeyPolicyBase>(comparer)
            {
                // Load custom content key policies
                { CustomContentKeyPolicies.CencDrmKey, new MediaServicesV3CustomContentKeyPolicyCencDrmKey(_amsDrmOpenIdConnectDiscoveryDocument, _amsDrmFairPlayPfxPassword, _amsDrmFairPlayAskHex, _amsDrmFairPlayPfx) },
                { CustomContentKeyPolicies.MultiDrmKey, new MediaServicesV3CustomContentKeyPolicyMultiDrmKey(_amsDrmOpenIdConnectDiscoveryDocument, _amsDrmFairPlayPfxPassword, _amsDrmFairPlayAskHex, _amsDrmFairPlayPfx) }
                // we can other custom content key policies here
            };
        }
    }
}
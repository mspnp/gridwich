using System;
using System.Collections.Generic;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies
{
    /// <summary>
    /// Concrete class that contains the logic and content key policy for the cenc DRM Content Key Policy.
    /// The policy will be updated ONLY IF the app settings "AmsDrmEnableContentKeyPolicyUpdate" is set to "true". Default is "false".
    /// If you changed the code below, you need either to activate the policy update, or delete the locators and delete the policy in the account. Then policy will be recreated.
    /// Supported DRM :
    /// - PlayReady
    /// - Widevine
    /// - NOT FairPlay (only CENC)
    /// </summary>
    public class MediaServicesV3CustomContentKeyPolicyCencDrmKey : MediaServicesV3CustomContentKeyPolicyBase
    {
        private readonly string drmTokenIssuer = ContentKeyPolicyClaims.DrmTokenIssuer;
        private readonly string drmTokenAudience = ContentKeyPolicyClaims.DrmTokenAudience;
        private readonly TimeSpan durationOf14Days = TimeSpan.FromDays(14);
        private readonly TimeSpan durationOf2Hours = TimeSpan.FromHours(2);

        /// <summary>
        /// Name of the policy in Gridwich. Do not change.
        /// </summary>
        private readonly string name = CustomContentKeyPolicies.CencDrmKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3CustomContentKeyPolicyCencDrmKey"/> class.
        /// </summary>
        /// <param name="openIdConnectDiscoveryDocument">The open Id configuration (URL).</param>
        /// <param name="fairPlayPfxPassword">The password for the FairPlay pfx.</param>
        /// <param name="fairPlayAskHex">The ASK key in hexadecimal format.</param>
        /// <param name="fairPlayPfx">The FairPlay certificate in PFX format.</param>
        public MediaServicesV3CustomContentKeyPolicyCencDrmKey(string openIdConnectDiscoveryDocument, string fairPlayPfxPassword, string fairPlayAskHex, string fairPlayPfx)
            : base(openIdConnectDiscoveryDocument, fairPlayPfxPassword, fairPlayAskHex, fairPlayPfx)
        {
            Create();
        }

        /// <inheritdoc cref="MediaServicesV3CustomContentKeyPolicyBase"/>
        protected override void Create()
        {
            ContentKeyPolicyOptions = GetPolicyOptions();
            Name = name;
        }

        /// <summary>
        /// Method used to get the policy options for the content key policy.
        /// </summary>
        /// <returns>List of options.</returns>
        private List<ContentKeyPolicyOption> GetPolicyOptions()
        {
            List<ContentKeyPolicyTokenClaim> requiredClaims_persistent_none = new List<ContentKeyPolicyTokenClaim>()
                {
                    ContentKeyPolicyTokenClaim.ContentKeyIdentifierClaim,
                    new ContentKeyPolicyTokenClaim("persistent", "none")
                };

            List<ContentKeyPolicyTokenClaim> requiredClaims_persistent_2Hours = new List<ContentKeyPolicyTokenClaim>()
                {
                    ContentKeyPolicyTokenClaim.ContentKeyIdentifierClaim,
                    new ContentKeyPolicyTokenClaim("persistent", "2Hours")
                };

            List<ContentKeyPolicyTokenClaim> requiredClaims_persistent_14Days = new List<ContentKeyPolicyTokenClaim>()
                {
                    ContentKeyPolicyTokenClaim.ContentKeyIdentifierClaim,
                    new ContentKeyPolicyTokenClaim("persistent", "14Days")
                };

            ContentKeyPolicyTokenRestriction restriction_persistent_none = GenerateRestriction(drmTokenIssuer, drmTokenAudience, requiredClaims_persistent_none);
            ContentKeyPolicyTokenRestriction restriction_persistent_2Hours = GenerateRestriction(drmTokenIssuer, drmTokenAudience, requiredClaims_persistent_2Hours);
            ContentKeyPolicyTokenRestriction restriction_persistent_14Days = GenerateRestriction(drmTokenIssuer, drmTokenAudience, requiredClaims_persistent_14Days);

            return new List<ContentKeyPolicyOption>
            {
                // PlayReady Persistent None
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigurePlayReadyLicenseTemplatePersistentNone(),
                    Restriction = restriction_persistent_none,
                    Name = "ContentKeyPolicyOption_PlayReady_Persistent_None"
                },

                 // PlayReady Persistent 2 Hours
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigurePlayReadyLicenseTemplatePersistent(durationOf2Hours),
                    Restriction = restriction_persistent_2Hours,
                    Name = "ContentKeyPolicyOption_PlayReady_Persistent_2Hours"
                },

                // PlayReady Persistent 14 Days
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigurePlayReadyLicenseTemplatePersistent(durationOf14Days),
                    Restriction = restriction_persistent_14Days,
                    Name = "ContentKeyPolicyOption_PlayReady_Persistent_14Days"
                },

                // Widevine Persistent None
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigureWidevineLicenseTemplatePersistentNone(),
                    Restriction = restriction_persistent_none,
                    Name = "ContentKeyPolicyOption_Widevine_Persistent_None"
                },

                 // Widevine Persistent 2 Hours
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigureWidevineLicenseTemplatePersistent(durationOf2Hours),
                    Restriction = restriction_persistent_2Hours,
                    Name = "ContentKeyPolicyOption_Widevine_Persistent_2Hours"
                },

                // Widevine Persistent 14 Days
                new ContentKeyPolicyOption()
                {
                    Configuration = ConfigureWidevineLicenseTemplatePersistent(durationOf14Days),
                    Restriction = restriction_persistent_14Days,
                    Name = "ContentKeyPolicyOption_Widevine_Persistent_14Days"
                }
            };
        }

        /// <summary>
        /// Configures Widevine license template for non persistent licenses.
        /// </summary>
        /// <returns>The Widevine template for non persistent licenses.</returns>
        private static ContentKeyPolicyWidevineConfiguration ConfigureWidevineLicenseTemplatePersistentNone()
        {
            WidevineTemplate template = new WidevineTemplate()
            {
                AllowedTrackTypes = "SD_HD",
                ContentKeySpecs = new ContentKeySpec[]
                {
                    new ContentKeySpec()
                    {
                        TrackType = "SD",
                        SecurityLevel = 1,
                        RequiredOutputProtection = new OutputProtection()
                        {
                            HDCP = "HDCP_NONE"
                        }
                    }
                },
                PolicyOverrides = new PolicyOverrides()
                {
                    CanPlay = true,
                    CanPersist = false,
                    CanRenew = false,
                }
            };

            return new ContentKeyPolicyWidevineConfiguration
            {
                WidevineTemplate = Newtonsoft.Json.JsonConvert.SerializeObject(template)
            };
        }

        /// <summary>
        /// Configures Widevine license template for persistent licenses.
        /// </summary>
        /// <returns>The Widevine template for persistent licenses.</returns>
        private static ContentKeyPolicyWidevineConfiguration ConfigureWidevineLicenseTemplatePersistent(TimeSpan licenseDuration)
        {
            WidevineTemplate template = new WidevineTemplate()
            {
                AllowedTrackTypes = "SD_HD",
                ContentKeySpecs = new ContentKeySpec[]
                {
                    new ContentKeySpec()
                    {
                        TrackType = "SD",
                        SecurityLevel = 1,
                        RequiredOutputProtection = new OutputProtection()
                        {
                            HDCP = "HDCP_NONE"
                        }
                    }
                },
                PolicyOverrides = new PolicyOverrides()
                {
                    CanPlay = true,
                    CanPersist = true,
                    CanRenew = false,
                    RentalDurationSeconds = Convert.ToInt32(licenseDuration.TotalSeconds),
                    PlaybackDurationSeconds = Convert.ToInt32(licenseDuration.TotalSeconds),
                    LicenseDurationSeconds = Convert.ToInt32(licenseDuration.TotalSeconds),
                }
            };

            return new ContentKeyPolicyWidevineConfiguration
            {
                WidevineTemplate = Newtonsoft.Json.JsonConvert.SerializeObject(template)
            };
        }

        /// <summary>
        /// Configures PlayReady license template for non persistent licenses.
        /// </summary>
        /// <returns>The PlayReady template for non persistent licenses.</returns>
        private static ContentKeyPolicyPlayReadyConfiguration ConfigurePlayReadyLicenseTemplatePersistentNone()
        {
            ContentKeyPolicyPlayReadyLicense objContentKeyPolicyPlayReadyLicense;

            objContentKeyPolicyPlayReadyLicense = new ContentKeyPolicyPlayReadyLicense
            {
                AllowTestDevices = false,
                ContentKeyLocation = new ContentKeyPolicyPlayReadyContentEncryptionKeyFromHeader(),
                ContentType = ContentKeyPolicyPlayReadyContentType.Unspecified,
                LicenseType = ContentKeyPolicyPlayReadyLicenseType.NonPersistent,
                PlayRight = new ContentKeyPolicyPlayReadyPlayRight
                {
                    AllowPassingVideoContentToUnknownOutput = ContentKeyPolicyPlayReadyUnknownOutputPassingOption.Allowed
                }
            };

            return new ContentKeyPolicyPlayReadyConfiguration
            {
                Licenses = new List<ContentKeyPolicyPlayReadyLicense> { objContentKeyPolicyPlayReadyLicense }
            };
        }

        /// <summary>
        /// Configures PlayReady license template for persistent licenses.
        /// </summary>
        /// <returns>The PlayReady template for persistent licenses.</returns>
        private static ContentKeyPolicyPlayReadyConfiguration ConfigurePlayReadyLicenseTemplatePersistent(TimeSpan licenseDuration)
        {
            ContentKeyPolicyPlayReadyLicense objContentKeyPolicyPlayReadyLicense;

            objContentKeyPolicyPlayReadyLicense = new ContentKeyPolicyPlayReadyLicense
            {
                AllowTestDevices = false,
                ContentKeyLocation = new ContentKeyPolicyPlayReadyContentEncryptionKeyFromHeader(),
                ContentType = ContentKeyPolicyPlayReadyContentType.Unspecified,
                RelativeExpirationDate = licenseDuration,
                LicenseType = ContentKeyPolicyPlayReadyLicenseType.Persistent,
                PlayRight = new ContentKeyPolicyPlayReadyPlayRight
                {
                    AllowPassingVideoContentToUnknownOutput = ContentKeyPolicyPlayReadyUnknownOutputPassingOption.Allowed
                }
            };

            return new ContentKeyPolicyPlayReadyConfiguration
            {
                Licenses = new List<ContentKeyPolicyPlayReadyLicense> { objContentKeyPolicyPlayReadyLicense }
            };
        }
    }
}

/*

    Examples of valid JWT Token

{
  "urn:microsoft:azure:mediaservices:contentkeyidentifier": "bed3b9e9-4dfc-4220-bd11-9312d6204712",
  "persistent": "none",
  "nbf": 1586946219,
  "exp": 1586947419,
  "iss": "gridwich",
  "aud": "urn:drm"
}

{
  "urn:microsoft:azure:mediaservices:contentkeyidentifier": "bed3b9e9-4dfc-4220-bd11-9312d6204712",
  "persistent": "14Days",
  "nbf": 1586946219,
  "exp": 1586947419,
  "iss": "gridwich",
  "aud": "urn:drm"
}

{
  "urn:microsoft:azure:mediaservices:contentkeyidentifier": "bed3b9e9-4dfc-4220-bd11-9312d6204712",
  "persistent": "2Hours",
  "nbf": 1586946219,
  "exp": 1586947419,
  "iss": "gridwich",
  "aud": "urn:drm"
}

*/

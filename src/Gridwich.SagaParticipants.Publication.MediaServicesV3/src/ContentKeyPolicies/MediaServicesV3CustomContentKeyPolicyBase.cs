using System;
using System.Collections.Generic;
using System.Linq;
using Gridwich.Core.Helpers;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies
{
    /// <summary>
    /// Class will be used as a base class to create new content key policies.
    /// </summary>
    public abstract class MediaServicesV3CustomContentKeyPolicyBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the policy has been created or updated in Azure Media Services.
        /// </summary>
        public bool CreatedOrUpdatedDone { get;  set; }

        /// <summary>
        /// Gets or sets a value of the default license duration (14 days is the default).
        /// </summary>
        protected TimeSpan DefaultPersistentLicenceDuration { get; set; }

        /// <summary>
        /// Gets or sets the name of content key policy.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the name of content key policy.
        /// </summary>
        public IEnumerable<ContentKeyPolicyOption> ContentKeyPolicyOptions { get; protected set; }

        /// <summary>
        /// Gets or sets the OpenID Connect Discovery Document (URL).
        /// </summary>
        protected string OpenIdConnectDiscoveryDocument { get; set; }

        /// <summary>
        /// Gets or sets the Fairplay Pfx password.
        /// </summary>
        protected string FairPlayPfxPassword { get; set; }

        /// <summary>
        /// Gets or sets the Fairplay Ask key in Hexadecimal format.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        protected byte[] FairPlayAskBytes { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the Fairplay Certificate in binary 64 format.
        /// </summary>
        protected string FairPlayPfx { get; set; }

        /// <summary>
        /// Method is used to create a content key policy.
        /// </summary>
        protected abstract void Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3CustomContentKeyPolicyBase"/> class.
        /// </summary>
        /// <param name="name">The name of the policy.</param>
        /// <param name="openIdConnectDiscoveryDocument">The open Id configuration (URL).</param>
        /// <param name="fairPlayPfxPassword">The password for the FairPlay pfx.</param>
        /// <param name="fairPlayAskHex">The ASK key in hexadecimal format.</param>
        /// <param name="fairPlayPfx">The FairPlay certificate in PFX4 format.</param>
        public MediaServicesV3CustomContentKeyPolicyBase(string openIdConnectDiscoveryDocument, string fairPlayPfxPassword, string fairPlayAskHex, string fairPlayPfx)
        {
            if (StringHelpers.NullIfNullOrWhiteSpace(fairPlayPfxPassword) == null)
            {
                const string Message = "fairPlayPfxPassword is null or whitespace.";
                throw new ArgumentException(Message);
            }
            if (StringHelpers.NullIfNullOrWhiteSpace(fairPlayAskHex) == null)
            {
                const string Message = "fairPlayAskHex is null or whitespace.";
                throw new ArgumentException(Message);
            }
            if (StringHelpers.NullIfNullOrWhiteSpace(fairPlayPfx) == null)
            {
                const string Message = "fairPlayPfx is null or whitespace.";
                throw new ArgumentException(Message);
            }

            OpenIdConnectDiscoveryDocument = openIdConnectDiscoveryDocument;
            FairPlayPfxPassword = fairPlayPfxPassword;
            FairPlayPfx = fairPlayPfx;

            try
            {
                FairPlayAskBytes = Enumerable
                      .Range(0, fairPlayAskHex.Length)
                      .Where(x => x % 2 == 0)
                      .Select(x => Convert.ToByte(fairPlayAskHex.Substring(x, 2), 16))
                      .ToArray();
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 DRM Config. Error when loading the FairPlay ASK.";
                throw new GridwichPublicationDRMConfigurationException(
                    msg,
                    ex);
            }

            // let's flag that the content key policy was not yet created/updated in the current session
            CreatedOrUpdatedDone = false;
        }

        /// <summary>
        /// Generates de restriction for the options.
        /// </summary>
        /// <param name="drmTokenIssuer">Issuer</param>
        /// <param name="drmTokenAudience">Audience</param>
        /// <param name="requiredClaims">Other required claims</param>
        /// <returns>The token restriction.</returns>
        protected ContentKeyPolicyTokenRestriction GenerateRestriction(string drmTokenIssuer, string drmTokenAudience, List<ContentKeyPolicyTokenClaim> requiredClaims)
        {
            return new ContentKeyPolicyTokenRestriction(
                issuer: drmTokenIssuer,
                audience: drmTokenAudience,
                primaryVerificationKey: null,
                restrictionTokenType: ContentKeyPolicyRestrictionTokenType.Jwt,
                alternateVerificationKeys: null,
                requiredClaims: requiredClaims,
                openIdConnectDiscoveryDocument: OpenIdConnectDiscoveryDocument);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests
{
    /// <summary>
    /// Tests for the Media Services V3 Service class implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]

    [TestClass]
    public class MediaServicesV3ContentKeyPolicyServiceTests
    {
        private static readonly IObjectLogger<MediaServicesV3ContentKeyPolicyService> LogKey = Mock.Of<IObjectLogger<MediaServicesV3ContentKeyPolicyService>>();
        private static readonly ISettingsProvider SettingsProvider = Mock.Of<ISettingsProvider>();


        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsData
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword, MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, true, null },
                    new object[] { null, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword, MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, false, typeof(ArgumentException) },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, null,  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, false, typeof(ArgumentException) },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword, null, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, false, typeof(ArgumentException) },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword, MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, null, false, typeof(ArgumentException) },
                };
            }
        }


        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsDataGetPolicy
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword,  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, "multiDrmKey", true, typeof(MediaServicesV3CustomContentKeyPolicyMultiDrmKey), null },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword,  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, "cencDrmKey", true, typeof(MediaServicesV3CustomContentKeyPolicyCencDrmKey), null },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword,  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, "notexistingpolicy", true, null, null },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword,  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, "baddata", "multiDrmKey", false, null, typeof(GridwichPublicationDRMConfigurationException) },
                    new object[] { MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument, "badpassword",  MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex, MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic, "multiDrmKey", false, null, typeof(GridwichPublicationDRMConfigurationException) },
                };
            }
        }

        /// <summary>
        /// Test MediaServicesV3ContentKeyPolicyService class with various settings.
        /// </summary>
        /// <param name="openId">OpenID value.</param>
        /// <param name="fairPlayPassword">FairPlay cert passwordt.</param>
        /// <param name="fairPlayAsk">FairPlay ASK key.</param>
        /// <param name="fairPlayCertSecretAsString">FairPlay cert as secret string.</param>
        /// <param name="expectedValue">Expected result.</param>
        /// <param name="typeException">Expected type of Exception.</param>
        [Theory]
        [MemberData(nameof(OperationsData))]
        public void MediaServicesV3ContentKeyPolicyServiceWithDRMSettings(string openId, string fairPlayPassword, string fairPlayAsk, string fairPlayCertSecretAsString, bool expectedValue, Type typeException)
        {
            Mock.Get(SettingsProvider)
           .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
           .Returns(openId);

            Mock.Get(SettingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayPfxPassword"))
             .Returns(fairPlayPassword);

            Mock.Get(SettingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayAskHex"))
             .Returns(fairPlayAsk);

            Mock.Get(SettingsProvider)
            .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayCertificateB64"))
            .Returns(fairPlayCertSecretAsString);

            if (expectedValue)
            {
                // Act
                var polService = new MediaServicesV3ContentKeyPolicyService(SettingsProvider, LogKey);

                // Assert
                Xunit.Assert.NotNull(polService);
            }
            else
            {
                _ = Xunit.Assert.Throws(typeException, () => new MediaServicesV3ContentKeyPolicyService(SettingsProvider, LogKey));
            }
        }

        /// <summary>
        /// Test GetContentKeyPolicyAsync() with various settings.
        /// </summary>
        /// <param name="openId">OpenID value.</param>
        /// <param name="fairPlayPassword">FairPlay cert passwordt.</param>
        /// <param name="fairPlayAsk">FairPlay ASK key.</param>
        /// <param name="fairPlayCertSecretAsString">FairPlay cert as secret string.</param>
        /// <param name="polName">Content key policy name.</param>
        /// <param name="expectedValue">Expected result.</param>
        /// <param name="typeResult">Expected type of result.</param>
        /// <param name="typeException">Expected type of Exception.</param>
        [Theory]
        [MemberData(nameof(OperationsDataGetPolicy))]
        public void MediaServicesV3ContentKeyPolicyServiceWithDRMSettingsGetContentKeyPolicyAsync(string openId, string fairPlayPassword, string fairPlayAsk, string fairPlayCertSecretAsString, string polName, bool expectedValue, Type typeResult, Type typeException)
        {
            Mock.Get(SettingsProvider)
           .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
           .Returns(openId);

            Mock.Get(SettingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayPfxPassword"))
             .Returns(fairPlayPassword);

            Mock.Get(SettingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayAskHex"))
             .Returns(fairPlayAsk);

            Mock.Get(SettingsProvider)
            .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayCertificateB64"))
            .Returns(fairPlayCertSecretAsString);

            // Act
            var polService = new MediaServicesV3ContentKeyPolicyService(SettingsProvider, LogKey);

            if (expectedValue)
            {
                var keyPol = polService.GetContentKeyPolicyFromMemory(polName);
                if (typeResult != null)
                {
                    Xunit.Assert.NotNull(keyPol);
                    Xunit.Assert.IsType(typeResult, keyPol);
                }
                else
                {
                    Xunit.Assert.Null(keyPol);
                }
            }
            else
            {
                _ = Xunit.Assert.Throws(typeException, () => polService.GetContentKeyPolicyFromMemory(polName));
            }
        }

        /// <summary>
        /// Test MediaServicesV3ContentKeyPolicyService() with null settings.
        /// </summary>
        [Fact]
        public void MediaServicesV3ContentKeyPolicyServiceWithNullSettings()
        {
            // Act
            var exception = Record.Exception(() => new MediaServicesV3ContentKeyPolicyService(null, LogKey));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        /// <summary>
        /// Test MediaServicesV3ContentKeyPolicyService() with null log.
        /// </summary>
        [Fact]
        public void MediaServicesV3ContentKeyPolicyServiceWithNullLog()
        {
            // Act
            var exception = Record.Exception(() => new MediaServicesV3ContentKeyPolicyService(SettingsProvider, null));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentNullException>();
        }
    }
}
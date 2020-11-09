using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies;
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
    public class MediaServicesV3StreamingPolicyServiceTests
    {
        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsDataGetPolicy
        {
            get
            {
                return new[]
                {
                    new object[] { "multiDrmStreaming", true, typeof(MediaServicesV3CustomStreamingPolicyMultiDrmStreaming), null },
                    new object[] { "cencDrmStreaming", true, typeof(MediaServicesV3CustomStreamingPolicyCencDrmStreaming), null },
                    new object[] { "notDeclaredPolicing", true, null, null },
                    new object[] { null, false, null, typeof(ArgumentNullException) },
                    new object[] { string.Empty, false, null, typeof(ArgumentException) }
                };
            }
        }

        /// <summary>
        /// Test MediaServicesV3StreamingPolicyService() constructor.
        /// </summary>
        [Fact]
        public void MediaServicesV3StreamingPolicyServiceWithDRMSettings()
        {
            // Act
            var streamingService = new MediaServicesV3CustomStreamingPolicyService();

            // Assert
            Xunit.Assert.NotNull(streamingService);
        }

        /// <summary>
        /// Test GetCustomStreamingPolicyFromMemory() with various settings.
        /// </summary>
        /// <param name="streamingPolicyName">Streaming policy name.</param>
        /// <param name="expectedValue">Expected result.</param>
        /// <param name="typeResult">Expected type of the result.</param>
        /// <param name="typeException">Expected type of Exception.</param>
        [Theory]
        [MemberData(nameof(OperationsDataGetPolicy))]
        public void MediaServicesV3StreamingPolicyServiceWithDRMSettingsGetContentKeyPolicyAsync(string streamingPolicyName, bool expectedValue, Type typeResult, Type typeException)
        {
            // Act
            var streamPolService = new MediaServicesV3CustomStreamingPolicyService();

            if (expectedValue)
            {
                var pol = streamPolService.GetCustomStreamingPolicyFromMemory(streamingPolicyName);
                if (typeResult != null)
                {
                    Xunit.Assert.NotNull(pol);
                    Xunit.Assert.IsType(typeResult, pol);
                }
                else
                {
                    Xunit.Assert.Null(pol);
                }
            }
            else
            {
                _ = Xunit.Assert.Throws(typeException, () => streamPolService.GetCustomStreamingPolicyFromMemory(streamingPolicyName));
            }
        }
    }
}
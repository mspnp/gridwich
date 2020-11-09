using System;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Microsoft.Azure.Management.Media.Models;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests.Transforms
{
    /// <summary>
    /// Test class is used to test the MediaServicesV3PresetTransform Class
    /// </summary>
    public class MediaServicesV3PresetTransformTests
    {
        /// <summary>
        /// Testing that the MediaServicesV3PresetTransform class can create a Preset transform.
        /// </summary>
        [Fact]
        public void MediaServicesV3PresetTransformCanCreatePresetTransformTest()
        {
            // Arrange
            string transformName = EncoderNamedPreset.AACGoodQualityAudio;
            var transform = new MediaServicesV3PresetTransform(transformName);

            // Act
            var output = transform.Output;

            // Assert
            Xunit.Assert.NotNull(output);
        }

        /// <summary>
        /// Testing that the MediaServicesV3PresetTransform class throws an exception when transform name is null.
        /// </summary>
        [Fact]
        public void MediaServicesV3PresetTransformWhenNullTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = null;

            var exception = Record.Exception(() => new MediaServicesV3PresetTransform(transformName));

            // Act
            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        /// <summary>
        /// Testing that the MediaServicesV3PresetTransform class throws an exception when transform name is empty.
        /// </summary>
        [Fact]
        public void MediaServicesV3PresetTransformWhenEmptyTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = string.Empty;

            var exception = Record.Exception(() => new MediaServicesV3PresetTransform(transformName));

            // Act
            // Assert
            Xunit.Assert.NotNull(exception);
        }
    }
}

using Gridwich.SagaParticipants.Encode.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests.Transforms
{
    /// <summary>
    /// Test class is used to test the MediaServicesV3TransformService Class
    /// </summary>
    public class MediaServicesV3TransformServiceTests
    {
        /// <summary>
        /// Testing that transforms can be found in the dictionary.
        /// </summary>
        /// <param name="transformName">Transform name.</param>
        [Theory]
        [InlineData("AACGoodQualityAudio")]
        [InlineData("AdaptiveStreaming")]
        [InlineData("ContentAwareEncoding")]
        [InlineData("H264MultipleBitrate1080p")]
        [InlineData("H264MultipleBitrate720p")]
        [InlineData("H264MultipleBitrateSD")]
        [InlineData("H264SingleBitrate1080p")]
        [InlineData("H264SingleBitrate720p")]
        [InlineData("H264SingleBitrateSD")]
        [InlineData(CustomTransforms.AudioMonoAacVideoMbrNoBFrames)]
        [InlineData(CustomTransforms.AudioCopyVideoMbrNoBFrames)]
        [InlineData(CustomTransforms.AudioCopyVideoMbr)]
        public void MediaServicesV3TransformServiceCanGetTransformsTest(string transformName)
        {
            // Arrange
            IMediaServicesV3TransformService service = new MediaServicesV3TransformService();

            // Act
            var transform = service.GetTransform(transformName);

            // Assert
            Xunit.Assert.NotNull(transform);
            Xunit.Assert.Equal(transform.Name, transformName);
            Xunit.Assert.NotNull(transform.Output);
        }

        /// <summary>
        /// Testing that invalid transforms cannot be found in the dictionary.
        /// </summary>
        [Fact]
        public void MediaServicesV3TransformServiceCannotGetInvalidTransformTest()
        {
            // Arrange
            const string transformName = "transformdoesntexist";
            IMediaServicesV3TransformService service = new MediaServicesV3TransformService();

            // Act
            var transform = service.GetTransform(transformName);

            // Assert
            Xunit.Assert.Null(transform);
        }

        /// <summary>
        /// Testing that the MediaServicesV3TransformService class throws an exception when transform name is null.
        /// </summary>
        [Fact]
        public void MediaServicesV3TransformServiceWhenNullTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = null;
            IMediaServicesV3TransformService service = new MediaServicesV3TransformService();

            var exception = Record.Exception(() => service.GetTransform(transformName));

            // Act
            // Assert
            Xunit.Assert.NotNull(exception);
        }

        /// <summary>
        /// Testing that the MediaServicesV3TransformService class throws an exception when transform name is empty.
        /// </summary>
        [Fact]
        public void MediaServicesV3TransformServiceWhenEmptyTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = string.Empty;
            IMediaServicesV3TransformService service = new MediaServicesV3TransformService();

            var exception = Record.Exception(() => service.GetTransform(transformName));

            // Act
            // Assert
            Xunit.Assert.NotNull(exception);
        }
    }
}

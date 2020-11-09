using System;
using System.Linq;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Microsoft.Azure.Management.Media.Models;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests.Transforms
{
    /// <summary>
    /// Test class is used to test the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform Class
    /// </summary>
    public class MediaServicesV3AudioCopyVideoMbrNoBFramesTransformTests
    {
        private const string TransformName = CustomTransforms.AudioCopyVideoMbrNoBFrames;

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class can create a Preset transform.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformCanCreateCustomTransformTest()
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;

            // Assert
            Xunit.Assert.NotNull(output);
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates a CopyAudio Codec audio layer.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformCopyAudioCodecTest()
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.Contains(preset.Codecs, p => p is CopyAudio);
        }


        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct number of H264 video layers.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformH264LayersTest()
        {
            // Arrange
            MediaServicesV3TransformBase transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);
            const int expectedNumberOfVideoLayers = 5;

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.Contains(preset.Codecs, p => p is H264Video);
            Xunit.Assert.NotNull(video);
            Xunit.Assert.Equal(expected: expectedNumberOfVideoLayers, video.Layers.Count);
        }


        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct H264 Layer label names.
        /// </summary>
        /// <param name="label">H264 Layer label</param>
        /// <param name="expected">Number of H264 Layers that match the label</param>
        [Theory]
        [InlineData("3840x2160", 0)]
        [InlineData("1920x1080", 2)]
        [InlineData("1280x720", 1)]
        [InlineData("960x540", 1)]
        [InlineData("640x360", 1)]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformH264LayerLabelTests(string label, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;
            var layers = video.Layers.Where(l => l.Label == label);

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.NotNull(video);
            Xunit.Assert.Equal(expected: expected, layers.Count());
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct H264 layer bitrates.
        /// </summary>
        /// <param name="bitrate">H264 bitrate</param>
        /// <param name="expected">Number of H264 Layers that match the bitrate</param>
        [Theory]
        [InlineData(16384000, 0)]
        [InlineData(4900000, 1)]
        [InlineData(2500000, 1)]
        [InlineData(1600000, 1)]
        [InlineData(800000, 1)]
        [InlineData(400000, 1)]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformH264LayerBitrateTests(int bitrate, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;
            var layers = video.Layers.Where(l => l.Bitrate == bitrate);

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.NotNull(video);
            Xunit.Assert.Equal(expected: expected, layers.Count());
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct H264 layer profiles.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expected">Number of H264 Layers that match the bitrate</param>
        [Theory]
        [InlineData("High", 2)]
        [InlineData("Main", 2)]
        [InlineData("Baseline", 1)]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformH264LayerProfileTests(string profile, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;
            var layers = video.Layers.Where(l => l.Profile == profile);

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.NotNull(video);
            Xunit.Assert.Equal(expected: expected, layers.Count());
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct BFrame count.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expectedBFrames">Number of BFrames expected for the Layer of that Profile</param>
        [Theory]
        [InlineData("High", 0)]
        [InlineData("Main", 0)]
        [InlineData("Baseline", null)]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformBFrameTests(string profile, int? expectedBFrames)
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;

            // Assert
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            preset.ShouldNotBeNull();
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;
            video.ShouldNotBeNull();
            var layers = video.Layers.Where(l => l.Profile == profile);
            layers.Count().ShouldNotBe(0);
            foreach (var item in layers)
            {
                item.BFrames.ShouldBe(expectedBFrames);
            }
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class creates the correct AdaptiveBFrame settings.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expectedAdaptiveBFrame">Expected AdaptiveBFrame setting for the Layer of that Profile</param>
        [Theory]
        [InlineData("High", false)]
        [InlineData("Main", false)]
        [InlineData("Baseline", false)]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformAdaptiveBFrameTests(string profile, bool expectedAdaptiveBFrame)
        {
            // Arrange
            var transform = new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;

            // Assert
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;
            preset.ShouldNotBeNull();
            var video = preset.Codecs.Where(p => p is H264Video).SingleOrDefault() as H264Video;
            video.ShouldNotBeNull();
            var layers = video.Layers.Where(l => l.Profile == profile);
            layers.Count().ShouldNotBe(0);
            foreach (var item in layers)
            {
                item.AdaptiveBFrame.ShouldBe(expectedAdaptiveBFrame);
            }
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class throws an exception when transform name is null.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformWhenNullTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = null;

            // Act
            var exception = Record.Exception(() => new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(transformName));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioCopyVideoMbrNoBFramesTransform class throws an exception when transform name is empty.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioCopyVideoMbrNoBFramesTransformWhenEmptyTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = string.Empty;

            // Act
            var exception = Record.Exception(() => new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(transformName));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentException>();
        }
    }
}

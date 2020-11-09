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
    /// Test class is used to test the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform Class
    /// </summary>
    public class MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformTests
    {
        private const string TransformName = CustomTransforms.AudioMonoAacVideoMbrNoBFrames;

        /// <summary>
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class can create a Preset transform.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformCanCreateCustomTransformTest()
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;

            // Assert
            Xunit.Assert.NotNull(output);
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates a AACAudio Codec audio layer.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformAacAudioCodecTest()
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

            // Act
            var output = transform.Output;
            var preset = output.TransformOutputs.SingleOrDefault().Preset as StandardEncoderPreset;

            // Assert
            Xunit.Assert.NotNull(preset);
            Xunit.Assert.Contains(preset.Codecs, p => p is AacAudio);
        }


        /// <summary>
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct number of H264 video layers.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformH264LayersTest()
        {
            // Arrange
            MediaServicesV3TransformBase transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);
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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct H264 Layer label names.
        /// </summary>
        /// <param name="label">H264 Layer label</param>
        /// <param name="expected">Number of H264 Layers that match the label</param>
        [Theory]
        [InlineData("3840x2160", 0)]
        [InlineData("1920x1080", 2)]
        [InlineData("1280x720", 1)]
        [InlineData("960x540", 1)]
        [InlineData("640x360", 1)]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformH264LayerLabelTests(string label, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct H264 layer bitrates.
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
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformH264LayerBitrateTests(int bitrate, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct H264 layer profiles.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expected">Number of H264 Layers that match the bitrate</param>
        [Theory]
        [InlineData("High", 2)]
        [InlineData("Main", 2)]
        [InlineData("Baseline", 1)]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformH264LayerProfileTests(string profile, int expected)
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct BFrame count.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expectedBFrames">Number of BFrames expected for the Layer of that Profile</param>
        [Theory]
        [InlineData("High", 0)]
        [InlineData("Main", 0)]
        [InlineData("Baseline", null)]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformBFrameTests(string profile, int? expectedBFrames)
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class creates the correct AdaptiveBFrame settings.
        /// </summary>
        /// <param name="profile">H264 profile</param>
        /// <param name="expectedAdaptiveBFrame">Expected AdaptiveBFrame setting for the Layer of that Profile</param>
        [Theory]
        [InlineData("High", false)]
        [InlineData("Main", false)]
        [InlineData("Baseline", false)]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformAdaptiveBFrameTests(string profile, bool expectedAdaptiveBFrame)
        {
            // Arrange
            var transform = new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(TransformName);

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
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class throws an exception when transform name is null.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformWhenNullTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = null;

            var exception = Record.Exception(() => new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(transformName));

            // Act
            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        /// <summary>
        /// Testing that the MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform class throws an exception when transform name is empty.
        /// </summary>
        [Fact]
        public void MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransformWhenEmptyTransformNameThrowsExceptionTest()
        {
            // Arrange
            string transformName = string.Empty;

            // Act
            var exception = Record.Exception(() => new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(transformName));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentException>();
        }
    }
}

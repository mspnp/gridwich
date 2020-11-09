using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <summary>
    /// Concrete class that contains the logic and Transform for the AudioMono custom Transform.  If changes need to be made,
    /// you should create a new custom class with a new name.
    /// </summary>
    public class MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform : MediaServicesV3TransformBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform"/> class.
        /// </summary>
        /// <param name="name">The name of the transform</param>
        public MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(string name)
            : base(name)
        {
            Create();
        }

        /// <inheritdoc cref="MediaServicesV3TransformBase"/>
        protected override void Create()
        {
            Codec[] codecs = GetCodecs();
            Format[] formats = GetFormats();

            TransformOutput[] outputs = new TransformOutput[]
            {
                new TransformOutput(new StandardEncoderPreset(codecs: codecs, formats: formats))
            };

            Output = new MediaServicesV3TransformOutput(outputs, Description);
        }

        /// <summary>
        /// Method used to get the codecs for the transform.
        /// </summary>
        /// <returns>Array of Azure Media Services V3 Codec.</returns>
        private static Codec[] GetCodecs()
        {
            // Common settings
            /// <summary>
            /// Explicitly set to 0: we don't want b-frames which, May-20-2020, were causing non-zero timestamps.
            /// </summary>
            int? bframes = 0;
            TimeSpan? keyFrameInterval = TimeSpan.FromSeconds(2);

            List<Codec> codecs = new List<Codec>
            {
                // Add audio codecs
                new AacAudio("audio128", 1, 48000, 128000, AacAudioProfile.AacLc)
            };

            // Add video codecs
            H264Layer[] h264Layers = new H264Layer[]
            {
                GetH264Layer(profile: H264VideoProfile.High, bitrate: 4900000, bframes: bframes, width: "1920", height: "1080", label: "1920x1080"),
                GetH264Layer(profile: H264VideoProfile.High, bitrate: 2500000, bframes: bframes, width: "1920", height: "1080", label: "1920x1080"),
                GetH264Layer(profile: H264VideoProfile.Main, bitrate: 1600000, bframes: bframes, width: "1280", height: "720", label: "1280x720"),
                GetH264Layer(profile: H264VideoProfile.Main, bitrate: 800000, bframes: bframes, width: "960", height: "540", label: "960x540"),
                GetH264Layer(profile: H264VideoProfile.Baseline, bitrate: 400000, bframes: null, width: "640", height: "360", label: "640x360")
            };
            codecs.Add(new H264Video(keyFrameInterval: keyFrameInterval, layers: h264Layers));

            return codecs.ToArray();
        }

        /// <summary>
        /// Method used to get a Azure Media Services V3 H264Layer
        /// </summary>
        /// <param name="profile">Video profile</param>
        /// <param name="bitrate">Video bitrate</param>
        /// <param name="width">Video width</param>
        /// <param name="height">Video height</param>
        /// <param name="label">Label to use for the video layer</param>
        /// <returns>Azure Media Services V3 H264Layer</returns>
        private static H264Layer GetH264Layer(H264VideoProfile profile, int bitrate, int? bframes, string width, string height, string label)
        {
            return new H264Layer()
            {
                BFrames = bframes,
                Profile = profile,
                Level = SharedH264Settings.Level,
                Bitrate = bitrate,
                MaxBitrate = bitrate,
                BufferWindow = SharedH264Settings.BufferWindow,
                Width = width,
                Height = height,
                AdaptiveBFrame = SharedH264Settings.AdaptiveBFrame,
                FrameRate = SharedH264Settings.FrameRate,
                Label = label,
                ReferenceFrames = SharedH264Settings.ReferenceFrames
            };
        }

        /// <summary>
        /// Method is used to get the formats for the transform.
        /// </summary>
        /// <returns>Array of Azure Media Services V3 Format.</returns>
        private static Format[] GetFormats()
        {
            return new Format[]
            {
                new Mp4Format(filenamePattern: "{Basename}_{Label}_{Bitrate}.mp4")
            };
        }

        /// <summary>
        /// Private static class used to hold common H264 Settings
        /// </summary>
        private static class SharedH264Settings
        {
            /// <summary>
            /// Gets AdaptiveBFrame
            /// Explicitly set to false: we don't want BFrames which, May-20-2020, were causing non-zero timestamps.
            /// </summary>
            public static bool? AdaptiveBFrame { get { return false; } }
            public static TimeSpan? BufferWindow { get { return TimeSpan.FromSeconds(5); } }
            public static string FrameRate { get { return "0/1"; } }
            public static string Level { get { return "Auto"; } }
            public static int? ReferenceFrames { get { return 3; } }
        }
    }
}

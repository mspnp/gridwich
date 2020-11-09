using System;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Helpers
{
    /// <summary>
    /// Class used to contain basic helper methods for Media Services V3
    /// </summary>
    public static class MediaServicesV3TransformHelpers
    {
        /// <summary>
        /// Array of Azure Media Service V3 EncoderNamedPreset
        /// </summary>
        public static readonly EncoderNamedPreset[] EncoderNamedPresets =
        {
                EncoderNamedPreset.AACGoodQualityAudio, EncoderNamedPreset.AdaptiveStreaming,
                EncoderNamedPreset.ContentAwareEncoding, EncoderNamedPreset.H264MultipleBitrate1080p,
                EncoderNamedPreset.H264MultipleBitrate720p, EncoderNamedPreset.H264MultipleBitrateSD, EncoderNamedPreset.H264SingleBitrate1080p,
                EncoderNamedPreset.H264SingleBitrate720p, EncoderNamedPreset.H264SingleBitrateSD
        };

        /// <summary>
        /// Method used to validate a parameter.  This should be moved to Gridwich.Core.
        /// </summary>
        /// <param name="paramValue">Parameter value.</param>
        /// <param name="paramName">Name of parameter.</param>
        public static void CheckArgumentNotNullOrEmpty(string paramValue, string paramName)
        {
            if (paramValue == null)
            {
                throw new ArgumentNullException(paramName);
            }
            else if (string.IsNullOrWhiteSpace(paramValue))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} parameter is empty or white space.", paramName));
            }
        }
    }
}

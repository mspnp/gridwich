using System;
using System.Collections.Generic;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Constants;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Helpers;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <inheritdoc/>
    public class MediaServicesV3TransformService : IMediaServicesV3TransformService
    {
        private readonly Dictionary<string, MediaServicesV3TransformBase> _transforms;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3TransformService"/> class.
        /// </summary>
        public MediaServicesV3TransformService()
        {
            _transforms = LoadTransforms();
        }

        /// <summary>
        /// Method used to get a transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>MediaServicesV3TransformBase</returns>
        public MediaServicesV3TransformBase GetTransform(string transformName)
        {
            MediaServicesV3TransformHelpers.CheckArgumentNotNullOrEmpty(transformName, nameof(transformName));

            // Search the dictionary
            if (!_transforms.TryGetValue(transformName, out var transform))
            {
                transform = null;
            }

            return transform;
        }

        /// <summary>
        /// Method is used to load a list of transforms in memory,.
        /// </summary>
        /// <returns>Dictionary with the transform name and transform</returns>
        private static Dictionary<string, MediaServicesV3TransformBase> LoadTransforms()
        {
            var transforms = new Dictionary<string, MediaServicesV3TransformBase>(StringComparer.InvariantCultureIgnoreCase);

            // Load preset transforms
            foreach (var preset in MediaServicesV3TransformHelpers.EncoderNamedPresets)
            {
                transforms.Add(preset, new MediaServicesV3PresetTransform(preset));
            }

            // Load custom transforms
            transforms.Add(CustomTransforms.AudioMonoAacVideoMbrNoBFrames, new MediaServicesV3AudioMonoAacVideoMbrNoBFramesTransform(CustomTransforms.AudioMonoAacVideoMbrNoBFrames));
            transforms.Add(CustomTransforms.AudioCopyVideoMbrNoBFrames, new MediaServicesV3AudioCopyVideoMbrNoBFramesTransform(CustomTransforms.AudioCopyVideoMbrNoBFrames));
            transforms.Add(CustomTransforms.AudioCopyVideoMbr, new MediaServicesV3AudioCopyVideoMbrTransform(CustomTransforms.AudioCopyVideoMbr));

            return transforms;
        }
    }
}

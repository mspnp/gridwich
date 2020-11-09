using Gridwich.SagaParticipants.Encode.MediaServicesV3.Helpers;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <summary>
    /// Concrete class that contains the logic and Transform for Preset Transforms.
    /// </summary>
    public class MediaServicesV3PresetTransform : MediaServicesV3TransformBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3PresetTransform"/> class.
        /// </summary>
        /// <param name="name">The name of the transform</param>
        public MediaServicesV3PresetTransform(string name)
            : base(name)
        {
            Create();
        }

        /// <inheritdoc cref="MediaServicesV3TransformBase"/>
        protected override void Create()
        {
            _ = $"The {Name} Preset.";

            // Create new transform
            Preset preset = new BuiltInStandardEncoderPreset()
            {
                PresetName = Name
            };

            Output = new MediaServicesV3TransformOutput(new TransformOutput[] { new TransformOutput(preset) },
                Description);
        }
    }
}

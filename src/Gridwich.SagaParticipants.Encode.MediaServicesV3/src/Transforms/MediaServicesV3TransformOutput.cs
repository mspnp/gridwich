using System.Collections.Generic;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <summary>
    /// Class used to contain the description and the outputs of a specific transform.
    /// </summary>
    public class MediaServicesV3TransformOutput
    {
        /// <summary>
        /// Gets the Description of the Transform.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the Outputs of the Transform.
        /// </summary>
        public IEnumerable<TransformOutput> TransformOutputs { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3TransformOutput"/> class.
        /// </summary>
        /// <param name="transformOutputs">List of Azure Media Services V3 TransformOutput</param>
        /// <param name="description">The description for the transform</param>
        public MediaServicesV3TransformOutput(IEnumerable<TransformOutput> transformOutputs, string description)
        {
            TransformOutputs = transformOutputs;
            Description = description;
        }
    }
}

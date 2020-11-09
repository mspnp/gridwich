using Gridwich.SagaParticipants.Encode.MediaServicesV3.Helpers;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <summary>
    /// Class will be used as a base class to create new tranforms.
    /// </summary>
    public abstract class MediaServicesV3TransformBase
    {
        /// <summary>
        /// Gets or sets the description of the tranform.
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets or sets the name of transform.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets MediaServicesV3TransformOutput.
        /// </summary>
        public MediaServicesV3TransformOutput Output { get; protected set; }

        /// <summary>
        /// Method is used to create a transform.
        /// </summary>
        protected abstract void Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3TransformBase"/> class.
        /// </summary>
        /// <param name="name">The name of the transform.</param>
        protected MediaServicesV3TransformBase(string name)
        {
            // Validate
            MediaServicesV3TransformHelpers.CheckArgumentNotNullOrEmpty(name, nameof(name));

            Name = name;
            Description = $"The {Name} Transform";
        }
    }
}

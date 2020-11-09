namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms
{
    /// <summary>
    /// Interface used to contain logic to load and retrieve transforms.
    /// </summary>
    public interface IMediaServicesV3TransformService
    {
        /// <summary>
        /// Method used to get a transform
        /// </summary>
        /// <param name="transformName">Name of transform</param>
        /// <returns>MediaServicesV3TransformBase</returns>
        public MediaServicesV3TransformBase GetTransform(string transformName);
    }
}

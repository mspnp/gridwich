using System;
using System.Threading.Tasks;
using Gridwich.Core.DTO;
using Gridwich.SagaParticipants.Encode;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3
{
    /// <summary>
    /// Manages the Azure Media Services Encoder business logic.
    /// </summary>
    public interface IMediaServicesV3Encoder
    {
        /// <summary>
        /// Manages the Azure Media Services Encoder business logic.
        /// </summary>
        /// <param name="encodeCreateDTO">The input encodeCreateDTO,</param>
        /// <returns>ServiceOperationResultMediaServicesV3Publish including WorkflowJobName and EncoderContext.</returns>
        /// <exception cref="ArgumentNullException">For a null DTO.</exception>
        /// <exception cref="GridwichEncodeCreateDataException">For invalid inputs.</exception>
        /// <exception cref="GridwichMediaServicesV3CreateTransformException">For problems during transform creation.</exception>
        /// <exception cref="GridwichMediaServicesV3CreateAssetException">For problems during asset creation.</exception>
        /// <exception cref="GridwichEncodeInvalidOutputContainerException">For problems during output container validation.</exception>
        /// <exception cref="GridwichEncodeCreateJobException">For problems during job creation.</exception>
        Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestMediaServicesV3EncodeCreateDTO encodeCreateDTO);
    }
}
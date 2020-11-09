using System;
using System.Threading.Tasks;
using Gridwich.Core.DTO;
using Gridwich.SagaParticipants.Encode;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// Manages the Azure Media Services Encoder business logic.
    /// </summary>
    public interface IMediaServicesV2Encoder
    {
        /// <summary>
        /// Manages the Azure Media Services Encoder business logic.
        /// </summary>
        /// <param name="encodeCreateDTO">The input encodeCreateDTO,</param>
        /// <returns>ServiceOperationResultMediaServicesV3Publish including WorkflowJobName and EncoderContext.</returns>
        /// <exception cref="Exception">TODO: Fill in the proper exception list.</exception>
        Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestMediaServicesV2EncodeCreateDTO encodeCreateDTO);
    }
}
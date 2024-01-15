using Gridwich.Core.DTO;
using Gridwich.SagaParticipants.Encode.Flip.Models;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Encode.Flip.Services
{
    /// <summary>
    /// FlipService interface
    /// </summary>
    public interface IFlipService
    {
        /// <summary>
        /// Encodes the create asynchronous.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <returns>
        ///   <see cref="Task{ServiceOperationResultEncodeDispatched}"/>
        /// </returns>
        public Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestFlipEncodeCreateDTO dto);

        /// <summary>
        /// Gets the encode information.
        /// </summary>
        /// <param name="flipEncodingCompleteData">The flip encoding complete data.</param>
        /// <returns>
        ///   <see cref="Encoding"/>
        /// </returns>
        public Telestream.Cloud.Flip.Model.Encoding GetEncodeInfo(FlipEncodingCompleteData flipEncodingCompleteData);
    }
}
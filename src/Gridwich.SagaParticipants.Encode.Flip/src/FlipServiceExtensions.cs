using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.Flip.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Encode.Flip
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class FlipServiceExtensions
    {
        /// <summary>
        /// Adds Flip Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddFlip(this IServiceCollection services)
        {
            services.AddScoped<IFlipService, FlipService>();
            services.AddTransient<IEventGridHandler, FlipStatusHandler>();
            services.AddTransient<IEventGridHandler, FlipServiceEncodeCreateHandler>();
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3;
using Gridwich.SagaParticipants.Encode.MediaServicesV3;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MediaServicesV3ServiceExtensions
    {
        /// <summary>
        /// Adds Media Services Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddMediaServicesV3(this IServiceCollection services)
        {
            services.AddScoped<IMediaServicesV3Encoder, MediaServicesV3Encoder>();
            services.AddSingleton<IMediaServicesV3SdkWrapper, MediaServicesV3SdkWrapper>();
            services.AddScoped<IMediaServicesV3EncodeService, MediaServicesV3EncodeService>();
            services.AddTransient<IEventGridHandler, MediaServicesV3EncoderStatusHandler>();
            services.AddTransient<IEventGridHandler, MediaServicesV3EncodeCreateHandler>();
            services.AddSingleton<IMediaServicesV3TransformService, MediaServicesV3TransformService>();
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Presets;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MediaServicesV2ServiceExtensions
    {
        /// <summary>
        /// Adds Media Services Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddMediaServicesV2(this IServiceCollection services)
        {
            services.AddTransient<IEventGridHandler, MediaServicesV2EncodeCreateHandler>();
            services.AddScoped<IMediaServicesV2Encoder, MediaServicesV2Encoder>();
            // services.AddScoped<IMediaServicesV2EncodeService, MediaServicesV2SdkEncodeService>();
            // services.AddSingleton<IMediaServicesV2SdkWrapper, MediaServicesV2SdkWrapper>();
            services.AddScoped<IMediaServicesV2EncodeService, MediaServicesV2RestEncodeService>();
            services.AddSingleton<IMediaServicesV2RestWrapper, MediaServicesV2RestWrapper>();
            services.AddTransient<IEventGridHandler, MediaServicesV2CallbackHandler>();
            services.AddSingleton<IMediaServicesPreset, MediaServicesPreset>();
        }
    }
}
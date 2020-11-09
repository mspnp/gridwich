using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.EventGridHandlers;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies;
using Microsoft.Extensions.DependencyInjection;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gridwich.SagaParticipants.Publication.MediaServicesV3Tests")]

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3
{
    /// <summary>
    /// This defines an extension method to add any DI related dependencies.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MediaServicesV3PublicationServiceExtensions
    {
        /// <summary>
        /// Adds Media Services Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddMediaServicesV3Publication(this IServiceCollection services)
        {
            services.AddTransient<IEventGridHandler, MediaServicesLocatorCreateHandler>();
            services.AddTransient<IEventGridHandler, MediaServicesLocatorDeleteHandler>();
            services.AddScoped<IMediaServicesV3PublicationService, MediaServicesV3PublicationService>();
            services.AddScoped<IMediaServicesV3CustomStreamingPolicyService, MediaServicesV3CustomStreamingPolicyService>();
            // Singleton below is important because we use a boolean to manage the update of the content key policy once per Azure function session.
            services.AddSingleton<IMediaServicesV3ContentKeyPolicyService, MediaServicesV3ContentKeyPolicyService>();
        }
    }
}
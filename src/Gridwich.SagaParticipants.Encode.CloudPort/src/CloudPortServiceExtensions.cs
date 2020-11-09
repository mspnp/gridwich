using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.CloudPort.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Encode.CloudPort
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class CloudPortServiceExtensions
    {
        /// <summary>
        /// Adds CloudPort Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddCloudPort(this IServiceCollection services)
        {
            services.AddScoped<ICloudPortService, CloudPortService>();
            services.AddTransient<IEventGridHandler, CloudPortStatusHandler>();
            services.AddTransient<IEventGridHandler, CloudPortEncodeCreateHandler>();
        }
    }
}
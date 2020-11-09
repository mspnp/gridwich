using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class TelestreamCloudServiceExtensions
    {
        /// <summary>
        /// Adds CloudPort Encoder specific dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddTelestreamCloud(this IServiceCollection services)
        {
            services.AddScoped<ITelestreamCloudClientProvider, TelestreamCloudClientProvider>();
            services.AddScoped<ITelestreamCloudStorageProvider, TelestreamCloudStorageProvider>();
        }
    }
}
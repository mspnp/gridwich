using Gridwich.Core;
using Gridwich.Core.EventGrid;
using Gridwich.Host.FunctionApp.Services;
using Gridwich.SagaParticipants.Analysis.MediaInfo;
using Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders;
using Gridwich.SagaParticipants.Encode.CloudPort;
using Gridwich.SagaParticipants.Encode.Flip;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Gridwich.SagaParticipants.Storage.AzureStorage;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.Host.FunctionApp
{
    /// <summary>
    /// Registers Gridwich services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class GridwichConfigureServices
    {
        /// <summary>
        /// Registers all Gridwich services.
        /// </summary>
        /// <param name="services">Service Collection from App.</param>
        /// <returns>Service Collection.</returns>
        public static IServiceCollection AddGridwichServices(this IServiceCollection services)
        {
            // Add Gridwich dependent services here:
            services.AddObjectLogger();
            services.AddSettingsService();
            services.AddMediaInfoService();
            services.AddStorageService();
            services.AddEventGridService();
            services.AddAnalysisService();
            services.AddStorageEventGridServices();
            services.AddTelestreamCloud();
            services.AddFlip();
            services.AddAzureStorageManagement();
            services.AddCloudPort();
            services.AddLazyCache();
            return services;
        }
    }
}
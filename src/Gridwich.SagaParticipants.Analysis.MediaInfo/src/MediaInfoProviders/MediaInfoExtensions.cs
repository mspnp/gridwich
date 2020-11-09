using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders
{
    /// <summary>
    /// Extension methods for services initialization.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MediaInfoExtensions
    {
        /// <summary>
        /// Adds MediaInfo dependent services to collection.
        /// </summary>
        /// <param name="services">IServiceCollection services.</param>
        public static void AddMediaInfoService(this IServiceCollection services)
        {
            services.AddTransient<IMediaInfoReportService, MediaInfoReportService>();
            services.AddTransient<IMediaInfoProvider, MediaInfoProvider>();
        }
    }
}

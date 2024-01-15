using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Analysis.MediaInfo.EventGridHandlers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo
{
    /// <summary>
    /// Extension methods to inject dependencies into IServiceCollection DI container.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MediaInfoServicesExtensions
    {
        /// <summary>
        /// Adds all dependencies which do file analysis.
        /// </summary>
        /// <param name="services">IServiceCollection services.</param>
        public static void AddAnalysisService(this IServiceCollection services)
        {
            services.AddTransient<IEventGridHandler, BlobAnalysisMediaInfoHandler>();
        }
    }
}

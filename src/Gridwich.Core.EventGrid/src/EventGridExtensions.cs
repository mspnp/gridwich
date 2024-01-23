using Gridwich.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.Core.EventGrid
{
    /// <summary>
    /// Extension method to inject dependencies into IServiceCollection DI container.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class EventGridExtensions
    {
        /// <summary>
        /// Adds all dependencies of the EventGridServices.
        /// </summary>
        /// <param name="services">IServiceCollection services.</param>
        public static void AddEventGridService(this IServiceCollection services)
        {
            services.AddSingleton<IEventGridClientProvider, EventGridClientProvider>();
            services.AddTransient<IEventGridDispatcher, EventGridDispatcher>();
            services.AddTransient<IEventGridPublisher, EventGridPublisher>();
        }
    }
}

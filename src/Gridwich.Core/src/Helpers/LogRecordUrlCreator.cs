using System;
using Gridwich.Core.Bases;
using Gridwich.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Creates a query for a log record
    /// </summary>
    /// <seealso cref="Gridwich.Core.Interfaces.IAppInsightsUrlCreator" />
    public class LogRecordUrlCreator : AppInsightsUrlCreatorBase
    {
        private readonly ISettingsProvider _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecordUrlCreator"/> class.
        /// </summary>
        /// <param name="settings">The application settings.</param>
        public LogRecordUrlCreator(ISettingsProvider settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public override Uri CreateUrl(string query)
        {
            var urlEncodedB64string = CompressAppInsightsQuery(query);

            // Compose the URL
            var result = $@"https://portal.azure.com/#blade/Microsoft_Azure_Monitoring_Logs/LogsBlade/resourceId/{_settings.GetAppSettingsValue(@"AppInsights_ResourceId")}/source/LogsBlade.AnalyticsShareLinkToQuery/q/{urlEncodedB64string}";

            return new Uri(result);
        }
    }

    /// <summary>
    /// Extensions for the LogRecordUrlCreator
    /// </summary>
    public static class LogRecordUrlCreatorExtensions
    {
        /// <summary>
        /// Adds the log record URL creator.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddLogRecordUrlCreator(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IAppInsightsUrlCreator), typeof(LogRecordUrlCreator));
        }
    }
}

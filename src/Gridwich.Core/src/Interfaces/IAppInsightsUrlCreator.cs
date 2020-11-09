using System;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// Interface specification for an Application Insights URL creator
    /// </summary>
    public interface IAppInsightsUrlCreator
    {
        /// <summary>
        /// Creates the Application Insights URL for the given query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>
        /// A valid URI to Application Insights which will run the given query.
        /// </returns>
        Uri CreateUrl(string query);
    }
}

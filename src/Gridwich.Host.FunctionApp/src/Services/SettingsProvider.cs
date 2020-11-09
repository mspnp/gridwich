using System;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.Host.FunctionApp.Services
{
    /// <summary>
    /// Provides settings to services by converting domain concerns to app settings values.
    /// </summary>
    public class SettingsProvider : ISettingsProvider
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public SettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the application settings value.
        /// </summary>
        /// <param name="appSettingsKey">The application settings key.</param>
        /// <returns>
        /// The value of the application setting
        /// </returns>
        /// <exception cref="ArgumentNullException">appSettingsKey</exception>
        public string GetAppSettingsValue(string appSettingsKey)
        {
            if (string.IsNullOrEmpty(appSettingsKey))
            {
                throw new ArgumentNullException(nameof(appSettingsKey));
            }

            string appSettingsValue = Environment.GetEnvironmentVariable(appSettingsKey);
            if (appSettingsValue == null)
            {
                appSettingsValue = _configuration.GetValue<string>(appSettingsKey);
            }

            return appSettingsValue;
        }
    }

    /// <summary>
    /// The settings extensions
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SettingsExtensions
    {
        /// <summary>
        /// Adds the settings service.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddSettingsService(this IServiceCollection services)
        {
            services.AddTransient<ISettingsProvider, SettingsProvider>();
        }
    }
}
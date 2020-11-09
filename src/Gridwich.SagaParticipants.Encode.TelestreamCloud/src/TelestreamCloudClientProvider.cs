using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Telestream.Cloud.Flip.Api;
using Telestream.Cloud.Notifications.Api;
using Telestream.Cloud.Stores.Api;
using Telestream.Cloud.VantageCloudPort.Api;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud
{
    /// <summary>
    /// The client provider to access Telestream Cloud services.
    /// </summary>
    public class TelestreamCloudClientProvider : ITelestreamCloudClientProvider
    {
        /// <summary>
        /// Apis and API keys related to the CloudPort API.
        /// </summary>
        private const string EnvKey = "TELESTREAMCLOUD_API_KEY";

        /// <summary>
        /// Gets or sets the Api to be used in CloudPort calls.
        /// </summary>
        public IVantageCloudPortApi CloudPortApi { get; set; }

        /// <summary>
        /// Gets or sets the Apiu to be used in Telestream Cloud storage reference operations.
        /// </summary>
        public IStoresApi CloudPortStoresApi { get; set; }

        /// <summary>
        /// Gets or sets the Api to be used for Telestream Cloud notification operations.
        /// </summary>
        public INotificationsApi CloudPortNotificationsApi { get; set; }

        /// <summary>
        /// Gets or sets the Api to use Telestream Cloud Flip services.
        /// </summary>
        public IFlipApi FlipApi { get; set; }

        private readonly ISettingsProvider _settingsProvider;
        private readonly IObjectLogger<TelestreamCloudClientProvider> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelestreamCloudClientProvider"/> class.
        /// </summary>
        /// <param name="settingsProvider">A settingsProvider to access account keys.</param>
        /// <param name="log">Standard logger object.</param>
        public TelestreamCloudClientProvider(ISettingsProvider settingsProvider, IObjectLogger<TelestreamCloudClientProvider> log)
        {
            _settingsProvider = settingsProvider;
            _log = log;
            InitializeSDKs();
        }
        private void InitializeSDKs()
        {
            var apiKey = _settingsProvider.GetAppSettingsValue(EnvKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var message = $"{EnvKey}: is null or empty";
                _log.LogEvent(LogEventIds.CloudPortApiError, message);
                throw new ArgumentNullException(LogEventIds.CloudPortApiError.Name, message);
            }
            Telestream.Cloud.Stores.Client.Configuration storesConfiguration = new Telestream.Cloud.Stores.Client.Configuration();
            storesConfiguration.ApiKey.Add("X-Api-Key", apiKey);
            CloudPortStoresApi = new StoresApi(storesConfiguration);

            // Notifications are not being used just yet, but we include them now for future possibilies.
            Telestream.Cloud.Notifications.Client.Configuration notificationsConfiguration = new Telestream.Cloud.Notifications.Client.Configuration();
            notificationsConfiguration.ApiKey.Add("X-Api-Key", apiKey);
            CloudPortNotificationsApi = new NotificationsApi(notificationsConfiguration);

            Telestream.Cloud.VantageCloudPort.Client.Configuration cloudPortConfiguration = new Telestream.Cloud.VantageCloudPort.Client.Configuration();
            cloudPortConfiguration.ApiKey.Add("X-Api-Key", apiKey);
            CloudPortApi = new Telestream.Cloud.VantageCloudPort.Api.VantageCloudPortApi(cloudPortConfiguration);

            Telestream.Cloud.Flip.Client.Configuration flipConfiguration = new Telestream.Cloud.Flip.Client.Configuration();
            flipConfiguration.ApiKey.Add("X-Api-Key", apiKey);
            FlipApi = new FlipApi(flipConfiguration);
        }
    }
}

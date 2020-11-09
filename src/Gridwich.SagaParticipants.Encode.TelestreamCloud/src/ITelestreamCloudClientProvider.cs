using Telestream.Cloud.Flip.Api;
using Telestream.Cloud.Notifications.Api;
using Telestream.Cloud.Stores.Api;
using Telestream.Cloud.VantageCloudPort.Api;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud
{
    /// <summary>
    /// TelestreamCloudClientProvider interface
    /// </summary>
    public interface ITelestreamCloudClientProvider
    {
        /// <summary>
        /// Gets or sets the cloud port API.
        /// </summary>
        IVantageCloudPortApi CloudPortApi { get; set; }

        /// <summary>
        /// Gets or sets the cloud port stores API.
        /// </summary>
        IStoresApi CloudPortStoresApi { get; set; }

        /// <summary>
        /// Gets or sets the cloud port notifications API.
        /// </summary>
        INotificationsApi CloudPortNotificationsApi { get; set; }

        /// <summary>
        /// Gets or sets the flip API.
        /// </summary>
        IFlipApi FlipApi { get; set; }
    }
}
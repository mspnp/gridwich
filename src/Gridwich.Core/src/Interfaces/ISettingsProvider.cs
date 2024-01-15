namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// An interface defining the structure of a provider for application settings
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Gets the application settings value.
        /// </summary>
        /// <param name="appSettingsKey">The application settings key.</param>
        /// <returns>The value of the application setting</returns>
        string GetAppSettingsValue(string appSettingsKey);
    }
}